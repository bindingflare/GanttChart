using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Edcore.GanttChart
{
    /// <summary>
    /// Wrapper ProjectManager class
    /// </summary>
    [Serializable]
    public class ProjectManager : ProjectManager<Task, object>
    {
        public ProjectManager(string projectName) : base(projectName)
        {
        }
    }

    /// <summary>
    /// Concrete ProjectManager class for the IProjectManager interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="R"></typeparam>
    [Serializable]
    public class ProjectManager<T, R> : IProjectManager<T, R>
        where T : Task
        where R : class
    {
        HashSet<T> m_registerSet = new HashSet<T>();
        List<T> m_RootTasks = new List<T>();
        Dictionary<T, List<T>> m_MembersOfGroup = new Dictionary<T, List<T>>(); // Map group to list of members
        Dictionary<T, HashSet<T>> m_dependantsOfPrecedentMap = new Dictionary<T, HashSet<T>>(); // Map precendent to list of dependents
        Dictionary<T, HashSet<R>> m_resourcesOfTaskMap = new Dictionary<T, HashSet<R>>(); // Map task to list of resources
        Dictionary<T, List<T>> m_partsOfSplitTaskMap = new Dictionary<T, List<T>>(); // Map split task to list of task parts
        Dictionary<T, T> m_splitTaskOfPartMap = new Dictionary<T, T>(); // Map a task part to the original split task
        Dictionary<T, T> m_groupOfMemberMap = new Dictionary<T, T>(); // Map member task to parent group task
        Dictionary<T, int> m_taskIndicesMap = new Dictionary<T, int>(); // Map the task to its zero-based index order position

        public List<Header> HeaderList; // Map the userFields to array position in Task.UserFields

        public float FieldMinSize = 20;
        public float FieldMaxSize = 1000;

        /// <summary>
        /// Create a new Project
        /// </summary>
        public ProjectManager(string projectName)
        {
            Now = TimeSpan.Zero;
            Start = DateTime.Now;
            Name = projectName;

            // Add default headers
            HeaderList = new List<Header>();
            HeaderList.Add(new Header("Name", "tree", 0, 200f));
            HeaderList.Add(new Header("ID", "string", 1, 60f, false, true));
            HeaderList.Add(new Header("Start", "date", 2, 125f));
            HeaderList.Add(new Header("End", "date", 3, 125f));
            HeaderList.Add(new Header("Duration", "time", 4, 80f));
            HeaderList.Add(new Header("Complete", "string", 5, 60f, true, true));
            HeaderList.Add(new Header("Delay", "string", 6, 80f));

            FieldCount = 7;
            MainFieldCount = 7;
        }

        /// <summary>
        /// Get or set the name of the project
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or set the TimeSpan we are at now from Start DateTime
        /// </summary>
        public TimeSpan Now { get; set; }

        /// <summary>
        /// Get the number of user fields
        /// </summary>
        public int FieldCount { get; private set; }

        /// <summary>
        /// Get the number of custom user fields
        /// </summary>
        public int MainFieldCount { get; private set; }

        /// <summary>
        /// Get or set the starting date for this project
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// Get the date after the specified TimeSpan
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public DateTime GetDateTime(TimeSpan span)
        {
            return this.Start.Add(span);
        }

        /// <summary>
        /// Create a new T for this Project and add it to the T tree
        /// </summary>
        /// <returns></returns>
        public void Add(T task)
        {
            if (!this.m_registerSet.Contains(task))
            {
                m_registerSet.Add(task);
                m_RootTasks.Add(task);
                m_MembersOfGroup[task] = new List<T>();
                m_dependantsOfPrecedentMap[task] = new HashSet<T>();
                m_resourcesOfTaskMap[task] = new HashSet<R>();
                m_groupOfMemberMap[task] = null;

                // Set custom fields size
                task.CustomFieldsData = new string[FieldCount - MainFieldCount]; // TODO Add overload for task with customfieldsdata
            }
        }

        /// <summary>
        /// Remove task from this Project
        /// </summary>
        /// <param name="task"></param>
        public void Delete(T task)
        {
            if (task != null
                && !m_splitTaskOfPartMap.ContainsKey(task) // not a task part
                )
            {
                // Check if is group so can ungroup the task
                if (this.IsGroup(task))
                    this.Ungroup(task);

                if (this.IsSplit(task))
                    this.Merge(task);

                // Really delete all references
                m_RootTasks.Remove(task);
                m_MembersOfGroup.Remove(task);
                m_dependantsOfPrecedentMap.Remove(task);
                m_resourcesOfTaskMap.Remove(task);
                m_groupOfMemberMap.Remove(task);
                m_partsOfSplitTaskMap.Remove(task);
                foreach (var g in m_MembersOfGroup) g.Value.Remove(task); // optimised: no need to check for contains
                foreach (var g in m_dependantsOfPrecedentMap) g.Value.Remove(task);
                m_registerSet.Remove(task);
            }
            else if (task != null
                && m_splitTaskOfPartMap.ContainsKey(task) // must be existing part
                )
            {
                var split = m_splitTaskOfPartMap[task];
                var parts = m_partsOfSplitTaskMap[split];
                if (parts.Count > 2)
                {
                    parts.Remove(task); // remove the part from the split task
                    m_registerSet.Remove(task); // unregister the part
                    m_resourcesOfTaskMap.Remove(task);
                    m_splitTaskOfPartMap.Remove(task); // remove the reverse lookup

                    split.Start = parts.First().Start; // recalculate the split task
                    split.End = parts.Last().End;
                    split.Duration = split.End - split.Start;
                }
                else
                {
                    this.Merge(split);
                }
            }
        }

        /// <summary>
        /// Add the member T to the group T
        /// </summary>
        /// <param name="group"></param>
        /// <param name="member"></param>
        public void Group(T group, T member)
        {
            if (group != null
                && member != null
                && m_registerSet.Contains(group)
                )
            {
                // if the member is a task part, assign the split task to the group instead
                if (m_splitTaskOfPartMap.ContainsKey(member)) member = m_splitTaskOfPartMap[member];

                if (m_registerSet.Contains(member)
                    && !group.Equals(member)
                    && !m_partsOfSplitTaskMap.ContainsKey(group) // group cannot be split task
                    && !m_splitTaskOfPartMap.ContainsKey(group) // group cannot be parts
                    && !this.MembersOf(member).Contains(group)
                    //&& !this.HasRelations(group) // WARNING: May break openly sourced code
                    )
                {
                    _DetachTask(member);

                    // add member to new group
                    m_MembersOfGroup[group].Add(member);
                    m_groupOfMemberMap[member] = group;

                    _RecalculateAncestorsSchedule();
                    _RecalculateSlack();
                    // clear indices since positions changed
                    m_taskIndicesMap.Clear();
                }
            }
        }

        /// <summary>
        /// Remove the member task from its group
        /// </summary>
        public void Ungroup(T group, T member)
        {
            if (group != null
                && member != null
                && m_registerSet.Contains(group)
                )
            {
                // change the member to become the split task is member is a task part
                if (m_splitTaskOfPartMap.ContainsKey(member)) member = m_splitTaskOfPartMap[member];
                if (m_registerSet.Contains(member) && this.IsGroup(group))
                {
                    var ancestor = this.GroupsOf(group).LastOrDefault();
                    if (ancestor == null) // group is in root
                        m_RootTasks.Insert(m_RootTasks.IndexOf(group) + 1, member);
                    else // group is not in root, we get the ancestor that is in root
                        m_RootTasks.Insert(m_RootTasks.IndexOf(ancestor) + 1, member);
                    m_MembersOfGroup[group].Remove(member);
                    m_groupOfMemberMap[member] = null;

                    _RecalculateAncestorsSchedule();
                }
            }
        }

        /// <summary>
        /// Ungroup all member task under the specfied group task. The specified group task will become a normal task.
        /// If the there is a grandparent group, the members will become members of the grandparent group.
        /// </summary>
        /// <param name="group"></param>
        public void Ungroup(T group)
        {
            List<T> memberList;
            if (group != null
                //&& _mRegister.Contains(group)
                && m_MembersOfGroup.TryGetValue(group, out memberList))
            {
                var newgroup = this.DirectGroupOf(group);
                if (newgroup == null)
                {
                    foreach (var member in memberList)
                    {
                        m_RootTasks.Add(member);
                        m_groupOfMemberMap[member] = null;
                    }
                }
                else
                {
                    foreach (var member in memberList)
                    {
                        m_MembersOfGroup[newgroup].Add(member);
                        m_groupOfMemberMap[member] = null;
                    }
                }

                memberList.Clear();

                _RecalculateAncestorsSchedule();
            }
        }

        /// <summary>
        /// Get the zero-based index of the task in this Project
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public int IndexOf(T task)
        {
            if (m_registerSet.Contains(task))
            {
                if (m_taskIndicesMap.ContainsKey(task))
                    return m_taskIndicesMap[task];

                int i = 0;
                foreach (var x in Tasks)
                {
                    if (x.Equals(task))
                    {
                        m_taskIndicesMap[task] = i;
                        return i;
                    }
                    i++;
                }
            }
            return -1;
        }

        /// <summary>
        /// Re-order position of the task by offset amount of places
        /// If task is moved between members, the task is added to the members' group
        /// If task is a member and it is moved above it's group or below last sibling member, then it is moved out of its group
        /// If task is a part, then its parent split-task will be move instead
        /// </summary>
        /// <param name="task"></param>
        /// <param name="offset"></param>
        public void Move(T task, int offset)
        {
            if (task != null && m_registerSet.Contains(task) && offset != 0)
            {
                if (IsPart(task)) task = SplitTaskOf(task);

                int indexoftask = IndexOf(task);
                if (indexoftask > -1)
                {
                    int newindexoftask = indexoftask + offset;
                    // check for out of index bounds
                    var taskcount = Tasks.Count();
                    if (newindexoftask < 0) newindexoftask = 0;
                    else if (newindexoftask > taskcount) newindexoftask = taskcount;
                    // get the index of the task that will be displaced
                    var displacedtask = Tasks.ElementAtOrDefault(newindexoftask);

                    if (displacedtask == task)
                    {
                        return;
                    }
                    if (displacedtask == null)
                    {
                        // adding to the end of the task list
                        _DetachTask(task);
                        m_RootTasks.Add(task);
                    }
                    else if (!displacedtask.Equals(task))
                    {
                        int indexofdestinationtask;
                        var displacedtaskparent = this.DirectGroupOf(displacedtask);
                        if (displacedtaskparent == null) // displacedtask is in root
                        {
                            indexofdestinationtask = m_RootTasks.IndexOf(displacedtask);
                            _DetachTask(task);
                            m_RootTasks.Insert(indexofdestinationtask, task);
                        }
                        else if (!displacedtaskparent.Equals(task)) // displaced task is not under the moving task
                        {
                            var memberlist = m_MembersOfGroup[displacedtaskparent];
                            indexofdestinationtask = memberlist.IndexOf(displacedtask);
                            _DetachTask(task);
                            memberlist.Insert(indexofdestinationtask, task);
                            m_groupOfMemberMap[task] = displacedtaskparent;
                        }
                    }

                    _RecalculateAncestorsSchedule();
                    _RecalculateSlack();

                    // clear indices since positions changed
                    m_taskIndicesMap.Clear();
                }
            }
        }

        /// <summary>
        /// Get the T tree
        /// </summary>
        public IEnumerable<T> Tasks
        {
            get
            {
                var stack = new Stack<T>(1024);
                var rstack = new Stack<T>(30);
                foreach (var task in m_RootTasks)
                {
                    stack.Push(task);
                    while (stack.Count > 0)
                    {
                        var visited = stack.Pop();
                        yield return visited;

                        foreach (var member in m_MembersOfGroup[visited])
                            rstack.Push(member);

                        while (rstack.Count > 0) stack.Push(rstack.Pop());
                    }
                }
            }
        }

        /// <summary>
        /// Get the children of the Root node
        /// </summary>
        public IEnumerable<T> RootTasks
        {
            get
            {
                var stack = new Stack<T>(1024);
                foreach (var task in m_RootTasks)
                {
                    stack.Push(task);
                    while (stack.Count > 0)
                    {
                        var visited = stack.Pop();
                        yield return visited;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerate upwards from member to and through all the parents and grandparents of the specified task
        /// </summary>
        public IEnumerable<T> GroupsOf(T member)
        {
            T parent = DirectGroupOf(member);
            while (parent != null)
            {
                yield return parent;
                parent = DirectGroupOf(parent);
            }
        }

        /// <summary>
        /// Enumerate through all the children and grandchildren of the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public IEnumerable<T> MembersOf(T group)
        {
            if (m_registerSet.Contains(group))
            {
                Stack<T> stack = new Stack<T>(20);
                Stack<T> rstack = new Stack<T>(10);
                foreach (var child in m_MembersOfGroup[group])
                {
                    stack.Push(child);
                    while (stack.Count > 0)
                    {
                        var visitedchild = stack.Pop();
                        yield return visitedchild;

                        // push the grandchild
                        rstack.Clear();
                        foreach (var grandchild in m_MembersOfGroup[visitedchild])
                            rstack.Push(grandchild);

                        // put in the right visiting order
                        while (rstack.Count > 0)
                            stack.Push(rstack.Pop());
                    }
                }
            }
        }

        /// <summary>
        /// Get the parent group of the specified task
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public T DirectGroupOf(T member)
        {
            if (m_groupOfMemberMap.ContainsKey(member)) // _mRegister.Contains(task))
            {
                return m_groupOfMemberMap[member];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Enumerate through all the direct children of the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public IEnumerable<T> DirectMembersOf(T group)
        {
            if (group == null) yield break;

            List<T> list;
            if (m_MembersOfGroup.TryGetValue(group, out list))
            {
                var iter = list.GetEnumerator();
                while (iter.MoveNext()) yield return iter.Current;
            }
        }

        /// <summary>
        /// Enumerate through all the direct precedents and indirect precedents of the specified task
        /// </summary>
        /// <param name="dependant"></param>
        /// <returns></returns>
        public IEnumerable<T> PrecedentsOf(T dependant)
        {
            if (m_registerSet.Contains(dependant))
            {
                var stack = new Stack<T>(20);
                foreach (var p in DirectPrecedentsOf(dependant))
                {
                    stack.Push(p);
                    while (stack.Count > 0)
                    {
                        var visited = stack.Pop();
                        yield return visited;
                        foreach (var grandp in DirectPrecedentsOf(visited))
                            stack.Push(grandp);
                    }
                }
            }
        }

        /// <summary>
        /// Enumerate through all the direct dependants and indirect dependants of the specified task
        /// </summary>
        /// <param name="precendent"></param>
        /// <returns></returns>
        public IEnumerable<T> DependantsOf(T precendent)
        {
            if (!m_dependantsOfPrecedentMap.ContainsKey(precendent)) yield break;

            var stack = new Stack<T>(20);
            foreach (var d in m_dependantsOfPrecedentMap[precendent])
            {
                stack.Push(d);
                while (stack.Count > 0)
                {
                    var visited = stack.Pop();
                    yield return visited;
                    foreach (var grandd in m_dependantsOfPrecedentMap[visited])
                        stack.Push(grandd);
                }
            }
        }

        /// <summary>
        /// Enumerate through all the direct precedents of the specified task
        /// </summary>
        /// <param name="dependants"></param>
        /// <returns></returns>
        public IEnumerable<T> DirectPrecedentsOf(T dependants)
        {
            return m_dependantsOfPrecedentMap.Where(x => x.Value.Contains(dependants)).Select(x => x.Key);
        }

        /// <summary>
        /// Enumerate through all the direct dependants of the specified task
        /// </summary>
        /// <param name="precedent"></param>
        /// <returns></returns>
        public IEnumerable<T> DirectDependantsOf(T precedent)
        {
            if (precedent == null) yield break;

            HashSet<T> dependants;
            if (m_dependantsOfPrecedentMap.TryGetValue(precedent, out dependants))
            {
                var iter = dependants.GetEnumerator();
                while (iter.MoveNext()) yield return iter.Current;
            }
        }

        /// <summary>
        /// Enumerate through all tasks that is a precedent, having dependants.
        /// </summary>
        public IEnumerable<T> Precedents
        {
            get { return m_dependantsOfPrecedentMap.Where(x => m_dependantsOfPrecedentMap[x.Key].Count > 0).Select(x => x.Key); }
        }

        /// <summary>
        /// Enumerate list of critical paths in Project
        /// </summary>
        public IEnumerable<IEnumerable<T>> CriticalPaths
        {
            get
            {
                Dictionary<TimeSpan, List<T>> endtimelookp = new Dictionary<TimeSpan, List<T>>(1024);
                TimeSpan max_end = TimeSpan.MinValue;
                foreach (var task in this.Tasks)
                {
                    List<T> list;
                    if (!endtimelookp.TryGetValue(task.End, out list))
                        endtimelookp[task.End] = new List<T>(10);
                    endtimelookp[task.End].Add(task);

                    if (task.End > max_end) max_end = task.End;
                }

                if (max_end != TimeSpan.MinValue)
                {
                    foreach (var task in endtimelookp[max_end])
                    {
                        yield return new T[] { task }.Concat(PrecedentsOf(task));
                    }
                }
            }
        }

        /// <summary>
        /// Get whether the specified task is a group
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool IsGroup(T task)
        {
            List<T> list;
            if (m_MembersOfGroup.TryGetValue(task, out list))
                return list.Count > 0;
            else
                return false;
        }

        /// <summary>
        /// Get whether the specified task is a member
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool IsMember(T task)
        {
            return this.DirectGroupOf(task) != null;
        }

        /// <summary>
        /// Get whether the specified task has relations, either has dependants or has precedents connecting to it.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool HasRelations(T task)
        {
            if (m_registerSet.Contains(task) && m_dependantsOfPrecedentMap.ContainsKey(task))
            {
                return m_dependantsOfPrecedentMap[task].Count > 0 || DirectPrecedentsOf(task).FirstOrDefault() != null;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Set a relation between the precedent and dependant task
        /// </summary>
        /// <param name="precedent"></param>
        /// <param name="dependant"></param>
        public void Relate(T precedent, T dependant)
        {
            if (m_registerSet.Contains(precedent)
                && m_registerSet.Contains(dependant)
                )
            {
                if (m_splitTaskOfPartMap.ContainsKey(precedent)) precedent = m_splitTaskOfPartMap[precedent];
                if (m_splitTaskOfPartMap.ContainsKey(dependant)) dependant = m_splitTaskOfPartMap[dependant];

                if (!precedent.Equals(dependant)
                    && !this.DependantsOf(dependant).Contains(precedent)
                    && !this.IsGroup(precedent)
                    && !this.IsGroup(dependant)
                    )
                {
                    m_dependantsOfPrecedentMap[precedent].Add(dependant);

                    _RecalculateDependantsOf(precedent);
                    _RecalculateAncestorsSchedule();
                    _RecalculateSlack();
                }
            }
        }

        /// <summary>
        /// Unset the relation between the precedent and dependant task, if any.
        /// </summary>
        /// <param name="precedent"></param>
        /// <param name="dependant"></param>
        public void Unrelate(T precedent, T dependant)
        {
            if (m_registerSet.Contains(precedent) && m_registerSet.Contains(dependant))
            {
                if (m_splitTaskOfPartMap.ContainsKey(precedent)) precedent = m_splitTaskOfPartMap[precedent];
                if (m_splitTaskOfPartMap.ContainsKey(dependant)) dependant = m_splitTaskOfPartMap[dependant];

                m_dependantsOfPrecedentMap[precedent].Remove(dependant);

                _RecalculateSlack();
            }
        }

        /// <summary>
        /// Remove all dependant task from specified precedent task
        /// </summary>
        /// <param name="precedent"></param>
        public void Unrelate(T precedent)
        {
            if (m_registerSet.Contains(precedent))
            {
                if (m_splitTaskOfPartMap.ContainsKey(precedent))
                    precedent = m_splitTaskOfPartMap[precedent];

                m_dependantsOfPrecedentMap[precedent].Clear();

                _RecalculateSlack();
            }
        }

        /// <summary>
        /// Assign the specified resource to the specified task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="resource"></param>
        public void Assign(T task, R resource)
        {
            if (m_registerSet.Contains(task) && !m_resourcesOfTaskMap[task].Contains(resource))
                m_resourcesOfTaskMap[task].Add(resource);
        }

        /// <summary>
        /// Unassign the specified resource from the specfied task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="resource"></param>
        public void Unassign(T task, R resource)
        {
            m_resourcesOfTaskMap[task].Remove(resource);
        }

        /// <summary>
        /// Unassign the all resources from the specfied task
        /// </summary>
        /// <param name="task"></param>
        public void Unassign(T task)
        {
            if (m_registerSet.Contains(task))
                m_resourcesOfTaskMap[task].Clear();
        }

        /// <summary>
        /// Unassign the specified resource from all tasks that has this resource assigned
        /// </summary>
        /// <param name="resource"></param>
        public void Unassign(R resource)
        {
            foreach (var r in m_resourcesOfTaskMap.Where(x => x.Value.Contains(resource)))
                r.Value.Remove(resource);
        }

        /// <summary>
        /// Enumerate through all the resources that has been assigned to some task.
        /// </summary>
        public IEnumerable<R> Resources
        {
            get
            {
                return m_resourcesOfTaskMap.SelectMany(x => x.Value).Distinct();
            }
        }


        /// <summary>
        /// Enumerate through all the resources that has been assigned to the specified task.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public IEnumerable<R> ResourcesOf(T task)
        {
            if (task == null || !m_registerSet.Contains(task))
                yield break;

            HashSet<R> list;
            if (m_resourcesOfTaskMap.TryGetValue(task, out list))
            {
                foreach (var item in list)
                    yield return item;
            }
        }

        /// <summary>
        /// Enumerate through all the tasks that has the specified resource assigned to it.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public IEnumerable<T> TasksOf(R resource)
        {
            return m_resourcesOfTaskMap.Where(x => x.Value.Contains(resource)).Select(x => x.Key);
        }

        /// <summary>
        /// Set the start value. Affects group start/end and dependants start time.
        /// </summary>
        public void SetStart(T task, TimeSpan value)
        {
            if (m_registerSet.Contains(task) && value != task.Start)
            {
                if (!this.IsGroup(task))
                {
                    _SetStartHelper(task, value);

                    _RecalculateAncestorsSchedule();
                    _RecalculateSlack();
                }
                // Set start for a group task
                else
                {
                    _SetGroupStartHelper(task, value);

                    _RecalculateAncestorsSchedule();
                    _RecalculateSlack();
                }
            }
        }

        /// <summary>
        /// Set the end time. Affects group end and dependants start time.
        /// </summary>
        public void SetEnd(T task, TimeSpan value)
        {
            if (m_registerSet.Contains(task) && value != task.End && !this.IsGroup(task))
            {
                this._SetEndHelper(task, value);

                _RecalculateAncestorsSchedule();
                _RecalculateSlack();
            }
        }

        /// <summary>
        /// Set the duration of the specified task from start to end.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="duration">Number of timescale units between ProjectManager.Start</param>
        public void SetDuration(T task, TimeSpan duration)
        {
            this.SetEnd(task, task.Start + duration);
        }

        /// <summary>
        /// Set the percentage complete of the specified task from 0.0f to 1.0f.
        /// No effect on group tasks as they will get the aggregated percentage complete of all child tasks
        /// </summary>
        /// <param name="task"></param>
        /// <param name="complete"></param>
        public void SetComplete(T task, float complete)
        {
            if (m_registerSet.Contains(task)
                && complete != task.Complete
                && !this.IsGroup(task) // not a group
                && !m_partsOfSplitTaskMap.ContainsKey(task) // not a split task
                )
            {
                _SetCompleteHelper(task, complete);

                _RecalculateComplete();
            }
        }

        /// <summary>
        /// Set whether to collapse the specified group task. No effect on regular tasks.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="collasped"></param>
        public void SetCollapse(T task, bool collasped)
        {
            if (m_registerSet.Contains(task) && this.IsGroup(task))
            {
                task.IsCollapsed = collasped;
            }
        }

        /// <summary>
        /// Split the specified task into consecutive parts part1 and part2.
        /// </summary>
        /// <param name="task">The regular task to split which has duration of at least 2 to make two parts of 1 time unit duration each.</param>
        /// <param name="part1">New Task part (1) of the split task, with the start time of the original task and the specified duration value.</param>
        /// <param name="part2">New Task part (2) of the split task, starting 1 time unit after part (1) ends and having the remaining of the duration of the origina task.</param>
        /// <param name="duration">The duration of part (1) will be set to the specified duration value but will also be adjusted to approperiate value if necessary.</param>
        public void Split(T task, T part1, T part2, TimeSpan duration)
        {
            if (task != null
                && part1 != null
                && part2 != null
                && !part1.Equals(part2) // parts cannot be the same
                && m_registerSet.Contains(task) // task must be registered
                && !m_partsOfSplitTaskMap.ContainsKey(task) // task must not already be a split task
                && !m_splitTaskOfPartMap.ContainsKey(task) // task must not be a task part
                && m_MembersOfGroup[task].Count == 0 // task cannot be a group
                && !m_registerSet.Contains(part1) // part1 and part2 must have never existed
                && !m_registerSet.Contains(part2)
                )
            {
                m_registerSet.Add(part1);  // register part1
                m_resourcesOfTaskMap[part1] = new HashSet<R>(); // create container for holding resource

                // add part1 to split task
                task.Complete = 0.0f; // reset the complete status
                var parts = m_partsOfSplitTaskMap[task] = new List<T>(2);
                parts.Add(part1);
                m_splitTaskOfPartMap[part1] = task; // make a reverse lookup

                // allign the schedule
                if (duration <= TimeSpan.Zero || duration >= task.Duration) duration = TimeSpan.FromTicks(task.Duration.Ticks / 2);
                part1.Start = task.Start;
                part1.End = task.End;
                part1.Duration = task.Duration;

                // split part1 to give part2
                this.Split(part1, part2, duration);
            }
        }

        /// <summary>
        /// Split the specified part and obtain another part from it.
        /// </summary>
        /// <param name="part">The task part to split which has duration of at least 2 to make two parts of 1 time unit duration each. Its duration will be set to the specified duration value.</param>
        /// <param name="other">New Task part of the original part, starting 1 time unit after it ends and having the remaining of the duration of the original part.</param>
        /// <param name="duration">The duration of part (1) will be set to the specified duration value but will also be adjusted to approperiate value if necessary.</param>
        public void Split(T part, T other, TimeSpan duration)
        {
            if (part != null
                && other != null
                && m_splitTaskOfPartMap.ContainsKey(part) // part must be an existing part
                && !m_registerSet.Contains(other) // other must not have existed
                )
            {
                m_registerSet.Add(other); // register other part
                m_resourcesOfTaskMap[other] = new HashSet<R>(); // create container for holding resource

                var split = m_splitTaskOfPartMap[part]; // get the split task
                var parts = m_partsOfSplitTaskMap[split]; // get the list of ordered parts

                parts.Insert(parts.IndexOf(part) + 1, other); // insert the other part after the existing part
                m_splitTaskOfPartMap[other] = split; // set the reverse lookup

                System.Diagnostics.Debug.Write("Project::Split(T part, T other, TimeSpan duration): Need to define minimum duration for splitting.");

                // limit the duration point within the split task duration
                if (duration <= TimeSpan.Zero || duration >= part.Duration) duration = TimeSpan.FromTicks(part.Duration.Ticks / 2);

                // the real split
                var one_duration = duration;
                var two_duration = part.Duration - duration;
                part.Duration = one_duration;
                part.End = part.Start + one_duration;
                other.Duration = two_duration;
                other.Start = part.End;
                other.End = other.Start + two_duration;

                _PackPartsForward(parts);
                split.Start = parts.First().Start; // recalculate the split task
                split.End = parts.Last().End;
                split.Duration = split.End - split.Start;

                _RecalculateDependantsOf(split);
                _RecalculateAncestorsSchedule();
            }
        }

        /// <summary>
        /// Join part1 and part2 in a split task into a single part represented by part1, and part2 will be deleted from the ProjectManager.
        /// The resulting part will have a duration total of the two parts.
        /// Part1 and part2 must be actual parts and must be consecutive parts in the split task.
        /// If the join results in only one part remaining, the all parts will be deleted and the split task will promote to a regular task
        /// Schedule of other parts will not be affected.
        /// TODO: Join option: EarlyStartLateEnd, EarlyStartEarlyEnd, LateStartLateEnd
        /// </summary>
        /// <param name="part1">The part to keep in the ProjectManager after the join completes successfully.</param>
        /// <param name="part2">The part to join into part1 and be deleted afterwards from the ProjectManager.</param>
        public void Join(T part1, T part2)
        {
            if (part1 != null
                && part2 != null
                && m_splitTaskOfPartMap.ContainsKey(part1) // part1 and part2 must already be existing parts
                && m_splitTaskOfPartMap.ContainsKey(part2)
                && m_splitTaskOfPartMap[part1] == m_splitTaskOfPartMap[part2] // part1 and part2 must be of the same split task
                )
            {

                var split = m_splitTaskOfPartMap[part1];
                var parts = m_partsOfSplitTaskMap[split];
                if (parts.Count > 2)
                {
                    // Aggregate part2 into part1, and determine join type
                    TimeSpan min; bool join_backwards;
                    if (part1.Start < part2.Start) { min = part1.Start; join_backwards = true; }
                    else { min = part2.Start; join_backwards = false; }
                    TimeSpan duration = part1.Duration + part2.Duration;

                    part1.Start = min;
                    part1.Duration = duration;
                    part1.End = min + duration;

                    // aggregate resouces
                    // TODO: Ask whether to aggregate resources?
                    foreach (var r in this.ResourcesOf(part2))
                        this.Assign(part1, r);
                    this.Unassign(part2);

                    // remove all traces of part2
                    parts.Remove(part2);
                    m_resourcesOfTaskMap.Remove(part2);
                    m_splitTaskOfPartMap.Remove(part2);
                    m_registerSet.Remove(part2);

                    // pack the remaining parts
                    if (join_backwards) _PackPartsForward(parts);
                    else _PackPartsBackwards(parts);

                    // set the duration
                    split.End = parts.Last().End;
                    split.Duration = split.End - split.Start;
                    split.Start = parts.First().Start;

                    _RecalculateAncestorsSchedule();
                }
                else
                {
                    this.Merge(split);
                }
            }
        }

        /// <summary>
        /// Merge all the parts of the splitted task back into one task, having duration equal to sum of total duration of individual task parts, and aggregating the resources onto the resulting task.
        /// </summary>
        /// <param name="split">The split Task to merge</param>
        public void Merge(T split)
        {
            if (split != null
                && m_partsOfSplitTaskMap.ContainsKey(split) // must be existing split task
                )
            {
                TimeSpan duration = TimeSpan.Zero;
                m_partsOfSplitTaskMap[split].ForEach(x =>
                {

                    // sum durations
                    duration += x.Duration;

                    // merge resources onto split task
                    foreach (var r in m_resourcesOfTaskMap[x])
                        this.Assign(split, r);

                    // remove traces of all parts
                    m_splitTaskOfPartMap.Remove(x);
                    m_registerSet.Remove(x);
                    m_resourcesOfTaskMap.Remove(x);
                });
                m_partsOfSplitTaskMap.Remove(split); // remove split as a split task

                // set the duration
                this.SetDuration(split, duration);
            }
        }

        /// <summary>
        /// Get the parts of the split task
        /// </summary>
        /// <param name="split"></param>
        /// <returns></returns>
        public IEnumerable<T> PartsOf(T split)
        {
            if (split != null
                && m_partsOfSplitTaskMap.ContainsKey(split) // must be existing split task
                )
            {
                return m_partsOfSplitTaskMap[split].Select(x => x);
            }
            else
            {
                return new T[0];
            }
        }

        /// <summary>
        /// Get the split task that the specified part belogs to.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public T SplitTaskOf(T part)
        {
            if (m_splitTaskOfPartMap.ContainsKey(part))
                return m_splitTaskOfPartMap[part];
            return null;
        }

        /// <summary>
        /// Get whether the specified task is a split task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool IsSplit(T task)
        {
            return task != null && m_partsOfSplitTaskMap.ContainsKey(task);
        }

        /// <summary>
        /// Get whether the specified task is a part of a split task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool IsPart(T task)
        {
            return task != null && m_splitTaskOfPartMap.ContainsKey(task);
        }

        /// <summary>
        /// Add a user-defined field of a task.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void AddCustomField(string name, string type, float size)
        {
            Header h = new Header(name, type, FieldCount, 150f);
            h.Size = size;
            HeaderList.Add(h);

            FieldCount++;

            // Update custom fields in each task
            int length = FieldCount - MainFieldCount;

            foreach (T task in Tasks)
            {
                var arr = task.CustomFieldsData;
                Array.Resize(ref arr, length);

                task.CustomFieldsData = arr;
            }
        }

        /// <summary>
        /// Removes a user-defined field of a task.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void RemoveCustomField(int index)
        {
            HeaderList.RemoveAt(index);

            FieldCount--;

            // Update custom fields in each task
            int length = FieldCount - MainFieldCount;
            foreach (T task in Tasks)
            {
                string[] oldData = task.CustomFieldsData;
                var newData = new string[length];

                int target = index - MainFieldCount;
                int modifier = 0;
                for (int i = 0; i < oldData.Length; i++)
                {
                    if (i != target)
                    {
                        newData[i + modifier] = oldData[i];
                    }
                    else
                    {
                        modifier--;
                    }
                }

                task.CustomFieldsData = newData;
            }
        }

        public object GetField(T task, int index)
        {
            switch (index)
            {
                case 0:
                    return task.Name;
                case 1:
                    return task.ID;
                case 2:
                    return GetDateTime(task.Start);
                case 3:
                    return GetDateTime(task.End);
                case 4:
                    return task.Duration;
                case 5:
                    return task.Complete;
                case 6:
                    return task.Delay;
                default:
                    return task.CustomFieldsData[index - MainFieldCount];
            }
        }

        public string GetFieldString(T task, int index)
        {
            switch (index)
            {
                case 0:
                    return task.Name;
                case 1:
                    return task.ID;
                case 2:
                    return GetDateTime(task.Start).ToString("yyyy.MM.dd hh:mm:ss");
                case 3:
                    return GetDateTime(task.End).ToString("yyyy.MM.dd hh:mm:ss");
                case 4:
                    return task.Duration.ToString(@"dd\.hh\:mm\:ss");
                case 5:
                    return task.Complete.ToString("%");
                default:
                    return task.CustomFieldsData[index - MainFieldCount];
            }
        }

        public string FormatData(object data)
        {
            if (data is string)
            {
                return (string)data;
            }
            else if (data is DateTime)
            {
                return ((DateTime)data).ToString("yyyy.MM.dd hh:mm:ss");
            }
            else if (data is TimeSpan)
            {
                return ((TimeSpan)data).ToString(@"dd\.hh\:mm\:ss");
            }
            else if (data is float)
            {
                return ((float)data).ToString("p");
            }
            else
            {
                if (data == null)
                {
                    return "";
                }
                else
                {
                    return data.ToString();
                }
            }
        }

        public List<string> GetHeaderNames()
        {
            List<string> names = new List<string>();

            foreach (Header h in HeaderList)
            {
                names.Add(h.Title);
            }

            return names;
        }

        /// <summary>
        /// Set a field's data using field header
        /// </summary>
        /// <param name="task"></param>
        /// <param name="index">Field index</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SetField(T task, Header header, string data)
        {
            return SetField(task, header.Index, data);
        }

        /// <summary>
        /// Set a field's data using field index
        /// </summary>
        /// <param name="task"></param>
        /// <param name="index">Field index</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SetField(T task, int index, string data)
        {
            switch (index)
            {
                case 0:
                    task.Name = data;
                    break;
                case 5:
                    task.Complete = float.Parse(data);
                    break;
                default:
                    // Custom field
                    task.CustomFieldsData[index - MainFieldCount] = data;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Set a field's data using field index
        /// </summary>
        /// <param name="task"></param>
        /// <param name="index">Field index</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SetField(T task, int index, TimeSpan data)
        {
            switch (index)
            {
                case 2:
                    SetStart(task, data);
                    break;
                case 3:
                    SetEnd(task, data);
                    break;
                case 4:
                    SetDuration(task, data);
                    break;
                case 6:
                    SetDelay(task, data);
                    break;
                default:
                    // Custom field
                    return false; // TODO: Allow more forms of cu
                    // break;
            }

            return true;
        }

        public void SetActualEnd(T task, TimeSpan actualEnd)
        {
            if(task.End < task.ActualEnd)
            {
                task.ActualEnd = actualEnd;
                task.Delay = task.ActualEnd - task.End;
            }
        }

        public void SetDelay(T task, TimeSpan delay)
        {
            if (m_splitTaskOfPartMap.ContainsKey(task))
            {
                // task part belonging to a split task needs special treatment
                var split = m_splitTaskOfPartMap[task];
                var parts = m_partsOfSplitTaskMap[split];
                
                split.ActualEnd = task.End + delay;
                split.Delay = delay;
                var last = parts.Last();
                last.ActualEnd = split.ActualEnd;
                last.Delay = split.Delay;
            }
            else // regular task or a split task, which we will treat normally
            {
                // Set actual end
                task.ActualEnd = task.End + delay;
                task.Delay = delay;
            }

            // If member, check if delay affects group
            if(IsMember(task))
            {
                var group = DirectGroupOf(task);

                // Check if delay proceeds up the grouping chain
                if(group.ActualEnd < task.ActualEnd)
                {
                    SetDelay(group, task.ActualEnd - group.ActualEnd);
                }
                else
                {
                    SetDelay(group, TimeSpan.Zero);
                }
            }   
        }

        public int GetFieldIndex(string name) // warning: for fields with duplicate names, can cause unwanted behavior
        {
            foreach (Header h in HeaderList)
            {
                if (h.Title.Equals(name))
                {
                    return h.Index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Detach the specified task from ProjectManager.Tasks (i.e. remove from its parent group, or if not it goes not have a parent group, unregister from root task status).
        /// The specified task will remain registered in ProjectManager.
        /// After execution of this helper method, the task is expected to be re-attached to ProjectManager.Tasks by regaining root task status, or joining a new group.
        /// </summary>
        /// <param name="task"></param>
        private void _DetachTask(T task)
        {
            var group = this.DirectGroupOf(task);
            if (group == null) // member is actually not in any group, so it must be in _mRootTasks
                m_RootTasks.Remove(task);
            else
            {
                m_MembersOfGroup[group].Remove(task);
                m_groupOfMemberMap[task] = null;
            }
        }

        private void _SetStartHelper(T task, TimeSpan value)
        {
            if (task.Start != value)
            {
                if (m_splitTaskOfPartMap.ContainsKey(task))
                {
                    // task part belonging to a split task needs special treatment
                    _SetPartStartHelper(task, value);
                }
                else // regular task or a split task, which we will treat normally
                {
                    // check out of bounds
                    if (value < TimeSpan.Zero) value = TimeSpan.Zero;
                    if (this.DirectPrecedentsOf(task).Any())
                    {
                        var max_end = this.DirectPrecedentsOf(task).Max(x => x.ActualEnd);
                        if (value <= max_end) value = max_end; // + One;
                    }

                    // save offset just in case we need to use for moving task parts
                    var offset = value - task.Start;

                    // cache value
                    task.Duration = task.End - task.Start;
                    task.Start = value;

                    // affect self
                    task.End = task.Start + task.Duration;
                    task.ActualEnd = task.End + task.Delay;

                    // calculate dependants
                    _RecalculateDependantsOf(task);

                    // shift the task parts accordingly if task was a split task
                    if (m_partsOfSplitTaskMap.ContainsKey(task))
                    {
                        m_partsOfSplitTaskMap[task].ForEach(x =>
                        {
                            x.Start += offset;
                            x.End += offset;
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Set the start date for a group task. The relative dates between the tasks in the group will not be affected
        /// </summary>
        /// <param name="group"></param>
        /// <param name="value"></param>
        private void _SetGroupStartHelper(T group, TimeSpan value)
        {
            // Check bounds
            if (value < TimeSpan.Zero) value = TimeSpan.Zero;
            if (m_registerSet.Contains(group) && value != group.Start && this.IsGroup(group))
            {
                bool earlier = value < group.Start;
                TimeSpan offset = value - group.Start;
                var decendants = earlier ? MembersOf(group).OrderBy((t) => t.Start) : MembersOf(group).OrderByDescending((t) => t.Start);

                foreach (T decendant in decendants)
                {
                    if (this.IsGroup(decendant)) continue;

                    decendant.Start += offset;
                    decendant.End += offset;

                    if (this.IsSplit(decendant))
                    {
                        var parts = m_partsOfSplitTaskMap[decendant];
                        foreach (T part in parts)
                        {
                            part.Start += offset;
                            part.End += offset;
                        }
                    }

                    _RecalculateDependantsOf(decendant);
                }

                _RecalculateAncestorsSchedule();
                _RecalculateSlack();
            }
        }

        private void _SetEndHelper(T task, TimeSpan value)
        {
            if (task.End != value)
            {
                if (m_splitTaskOfPartMap.ContainsKey(task))
                {
                    // task part belonging to a split task needs special treatment
                    _SetPartEndHelper(task, value);
                }
                else // regular task or a split task, which we will treat normally
                {
                    // check bounds
                    bool isSplitTask = m_partsOfSplitTaskMap.ContainsKey(task);
                    T last_part = null;
                    if (isSplitTask)
                    {
                        last_part = m_partsOfSplitTaskMap[task].Last();
                        if (value <= last_part.Start) value = last_part.Start + TimeSpan.FromMinutes(30);
                    }
                    if (value <= task.Start) value = task.Start + TimeSpan.FromMinutes(30); // end cannot be less than start

                    // assign end value
                    task.End = value;
                    task.Duration = task.End - task.Start;
                    task.ActualEnd = task.End + task.Delay;

                    _RecalculateDependantsOf(task);

                    if (isSplitTask)
                    {
                        last_part.End = value;
                        last_part.Duration = last_part.End - last_part.Start;
                        last_part.ActualEnd = last_part.End + last_part.Delay;
                    }
                }
            }
        }

        private void _SetPartStartHelper(T part, TimeSpan value)
        {
            var split = m_splitTaskOfPartMap[part];
            var parts = m_partsOfSplitTaskMap[split];

            // check bounds
            if (this.DirectPrecedentsOf(split).Any())
            {
                var max_end = this.DirectPrecedentsOf(split).Max(x => x.ActualEnd);
                if (value < max_end) value = max_end;
            }
            if (value < TimeSpan.Zero) value = TimeSpan.Zero;

            // flag whether we need to pack parts forward or backwards
            bool backwards = value < part.Start;

            // assign start value, maintining duration and modifying end
            var duration = part.End - part.Start;
            part.Start = value;
            part.End = value + duration;
            part.ActualEnd = part.End + part.Delay;

            // pack packs
            if (backwards) _PackPartsBackwards(parts);
            else _PackPartsForward(parts);

            // recalculate the split
            split.Start = parts.First().Start; // recalculate the split task
            split.End = parts.Last().End;
            split.Duration = split.End - split.Start;
            split.ActualEnd = split.End + split.Delay;

            _RecalculateDependantsOf(split);
        }

        private void _SetPartEndHelper(T part, TimeSpan value)
        {
            var split = m_splitTaskOfPartMap[part];
            var parts = m_partsOfSplitTaskMap[split];

            // check for bounds
            if (value <= part.Start) value = part.Start + TimeSpan.FromMinutes(30);

            // flag whether duration is increased or reduced
            bool increased = value > part.End;

            // set end value and duration
            part.End = value;
            part.Duration = part.End - part.Start;
            part.ActualEnd = part.End + part.Delay;

            // pack parts
            if (increased) _PackPartsForward(parts);

            // recalculate the split
            split.Start = parts.First().Start; // recalculate the split task
            split.End = parts.Last().End;
            split.Duration = split.End - split.Start;
            split.ActualEnd = split.End + split.Delay;

            _RecalculateDependantsOf(split);
        }

        private void _PackPartsBackwards(List<T> parts)
        {
            // pack backwards first before packing forward again
            for (int i = parts.Count - 2; i > 0; i--) // Cannot pack beyond first part (i > 0)
            {
                var earlier = parts[i];
                var later = parts[i + 1];
                if (later.Start <= earlier.End)
                {
                    earlier.End = later.Start;
                    earlier.Start = earlier.End - earlier.Duration;
                }
            }

            _PackPartsForward(parts);
        }

        private void _PackPartsForward(List<T> parts)
        {
            for (int i = 1; i < parts.Count; i++)
            {
                var current = parts[i];
                var previous = parts[i - 1];
                if (previous.End >= current.Start)
                {
                    current.Start = previous.End;
                    current.End = current.Start + current.Duration;
                }
            }
        }

        private void _SetCompleteHelper(T task, float value)
        {
            if (task.Complete != value)
            {
                if (value > 1) value = 1;
                else if (value < 0) value = 0;
                task.Complete = value;

                if (m_splitTaskOfPartMap.ContainsKey(task))
                {
                    var split = m_splitTaskOfPartMap[task];
                    var parts = m_partsOfSplitTaskMap[split];
                    float complete = 0;
                    TimeSpan duration = TimeSpan.Zero;
                    foreach (var part in parts)
                    {
                        complete += part.Complete * part.Duration.Ticks;
                        duration += part.Duration;
                    }
                    split.Complete = complete / duration.Ticks;
                }
            }
        }

        private void _RecalculateComplete()
        {
            Stack<T> groups = new Stack<T>();
            foreach (var task in m_RootTasks.Where(x => this.IsGroup(x)))
            {
                _RecalculateCompletedHelper(task);
            }
        }

        private float _RecalculateCompletedHelper(T groupOrSplit)
        {
            float t_complete = 0;
            TimeSpan t_duration = TimeSpan.Zero;

            if (m_partsOfSplitTaskMap.ContainsKey(groupOrSplit))
            {
                foreach (var part in m_partsOfSplitTaskMap[groupOrSplit])
                {
                    t_complete += part.Complete * part.Duration.Ticks;
                    t_duration += part.Duration;
                }

                // Add delay from complete of last part
                var lastPart = m_partsOfSplitTaskMap[groupOrSplit].Last();
                t_complete += lastPart.Complete * groupOrSplit.Delay.Ticks;
                t_duration += lastPart.Delay;
            }
            else
            {
                foreach (var member in this.DirectMembersOf(groupOrSplit))
                {
                    var memberDuration = member.Duration + member.Delay;

                    t_duration += memberDuration;
                    if (this.IsGroup(member)) t_complete += _RecalculateCompletedHelper(member) * memberDuration.Ticks;
                    else t_complete += member.Complete * memberDuration.Ticks;
                }
            }

            groupOrSplit.Complete = t_complete / t_duration.Ticks;


            return groupOrSplit.Complete;
        }

        private void _RecalculateDependantsOf(T precedent)
        {
            // affect decendants
            foreach (var dependant in this.DirectDependantsOf(precedent))
            {
                if (dependant.Start < precedent.ActualEnd)
                    this._SetStartHelper(dependant, precedent.ActualEnd);
            }
        }

        private void _RecalculateAncestorsSchedule()
        {
            // affects parent group
            foreach (var group in m_RootTasks.Where(x => this.IsGroup(x)))
            {
                _RecalculateAncestorsScheduleHelper(group);
            }
        }

        private void _RecalculateAncestorsScheduleHelper(T group)
        {
            // Reset delay
            group.Delay = TimeSpan.Zero;

            float t_complete = 0;
            TimeSpan t_duration = TimeSpan.Zero;
            var start = TimeSpan.MaxValue;
            var end = TimeSpan.MinValue;
            var delay = TimeSpan.Zero;

            foreach (var member in DirectMembersOf(group))
            {
                if (IsGroup(member))
                    _RecalculateAncestorsScheduleHelper(member);

                delay += member.Delay;
                t_duration += member.Duration + member.Delay;
                t_complete += member.Complete * (member.Duration + member.Delay).Ticks;
                if (member.Start < start)
                    start = member.Start;
                if (member.ActualEnd > end)
                    end = member.ActualEnd;
            }

            _SetStartHelper(group, start);
            _SetEndHelper(group, end);
            _SetCompleteHelper(group, t_complete / t_duration.Ticks);

            // Calculations on delay
            TimeSpan proportionalDelay = TimeSpan.FromTicks(Convert.ToInt64(((float) delay.Ticks / t_duration.Ticks) * group.Duration.Ticks));

            group.ActualEnd = group.End;
            group.Duration -= proportionalDelay;
            group.End -= proportionalDelay;
            group.Delay = proportionalDelay;
        }

        private void _RecalculateSlack()
        {
            var max_end = this.Tasks.Max(x => x.End);
            foreach (var task in this.Tasks)
            {
                // affects slack for current task
                if (this.DirectDependantsOf(task).Any())
                {
                    // slack until the earliest dependant needs to start
                    var min = this.DirectDependantsOf(task).Min(x => x.Start);
                    task.Slack = min - task.End;
                }
                else
                {
                    // no dependants, so we have all the time until the last task ends
                    task.Slack = max_end - task.End;
                }
            }
        }
    }
}
