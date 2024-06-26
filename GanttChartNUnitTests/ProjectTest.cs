﻿using System;
using System.Linq;
using NUnit.Framework;
using Edcore.GanttChart;

namespace GanttChartNUnitTests
{
    [TestFixture]
    public class ProjectTest
    {
        [Test]
        public void CreateAndRemoveTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");

            // create first task
            var first = new Task();
            manager.Add(first);
            Assert.IsTrue(manager.Tasks.Count() == 1, string.Format("{0} != {1}", 1, manager.Tasks.Count()));
            Assert.IsTrue(manager.DirectGroupOf(first) == null);

            // create second task, remove first task
            var second = new Task() { Name = "Apple Jack" };
            manager.Add(second);
            manager.Delete(first);
            var firstordefault = manager.Tasks.FirstOrDefault();
            Assert.IsTrue(firstordefault != null);
            Assert.IsTrue(firstordefault.Name == "Apple Jack");
            Assert.IsTrue(firstordefault.Equals(second));

            // remove a task that is already removed
            manager.Delete(first);
            Assert.IsTrue(manager.Tasks.Count() == 1);

            // remove a null task
            manager.Delete(null);
            Assert.IsTrue(manager.Tasks.Count() == 1);
        }

        [Test]
        public void CreateAndRemoveTaskParts()
        {
            var manager = new ProjectManager<Task, object>("Testing");

            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();

            manager.Add(split);
            manager.Add(part1);
            manager.Add(part2);
            Assert.IsTrue(manager.Tasks.Any(t => t.Equals(split)));
            Assert.IsTrue(manager.Tasks.Any(t => t.Equals(part1)));
            Assert.IsTrue(manager.Tasks.Any(t => t.Equals(part2)));

            // test: split task into part1 and part2, expect to fail.
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(split)) == 1);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part1)) == 1);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part2)) == 1);
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part1));

            // test: delete the parts
            manager.Delete(part1);
            manager.Delete(part2);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(split)) == 1);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part1)) == 0);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part2)) == 0);

            // test: split task into part1 and part2, expect to succeed.
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(split)) == 1);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part1)) == 0);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part2)) == 0);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));

            // test: split task part into part1 and part3
            manager.Split(part1, part3, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(split)) == 1);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part1)) == 0);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part2)) == 0);
            Assert.IsTrue(manager.Tasks.Count(t => t.Equals(part3)) == 0);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
        }

        [Test]
        public void CreateAndRemoveGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var one = new Task();
            var two = new Task();
            manager.Add(group1);
            manager.Add(one);
            manager.Add(two);

            // make group
            manager.Group(group1, one);
            manager.Group(group1, two);
            Assert.IsTrue(manager.Tasks.Count() == 3);
            Assert.IsTrue(manager.DirectMembersOf(group1).Count() == 2);
            Assert.IsTrue(manager.DirectGroupOf(one).Equals(group1));
            Assert.IsTrue(manager.DirectGroupOf(two).Equals(group1));
            
            // delete group task
            manager.Delete(group1);
            Assert.IsTrue(manager.Tasks.Count() == 2);
            Assert.IsTrue(manager.DirectMembersOf(group1).Count() == 0);
            Assert.IsTrue(manager.DirectGroupOf(one) == null);
            Assert.IsTrue(manager.DirectGroupOf(two) == null);   
        }

        [Test]
        public void ProjectEmptyEnumerators()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");

            Assert.IsTrue(manager.Tasks.Count() == 0, string.Format("count == {0} != {1}",manager.Tasks.Count(), 0) );
            Assert.IsTrue(manager.Resources.Count() == 0);
        }

        [Test]
        public void EnumerationShouldReturnEmptySet()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var alien = new Task();

            // test: Enumerators should at least return empty sets
            Assert.IsNotNull(manager.DirectMembersOf(alien), "ChildrenOf is null");
            Assert.IsNotNull(manager.GroupsOf(alien), "AncestorsOf is null");
            Assert.IsNotNull(manager.MembersOf(alien), "DecendantsOf is null");
            Assert.IsNotNull(manager.DependantsOf(alien), "DependantsOf is null");
            Assert.IsNotNull(manager.PrecedentsOf(alien), "PrecedentsOf is null");
            Assert.IsNotNull(manager.DirectDependantsOf(alien), "DirectDependantsOf is null");
            Assert.IsNotNull(manager.DirectPrecedentsOf(alien), "DirectPrecedentsOf is null");
            Assert.IsNotNull(manager.ResourcesOf(alien), "ResourcesOf is null");
            Assert.IsNotNull(manager.TasksOf(alien), "TasksOf is null");
        }

        [Test]
        public void KnownTasksEnumeration()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var local = new Task();
            manager.Add(local);

            Assert.IsNotNull(manager.DirectMembersOf(local));
            Assert.IsNotNull(manager.GroupsOf(local));
            Assert.IsNotNull(manager.MembersOf(local));
            Assert.IsNotNull(manager.DependantsOf(local));
            Assert.IsNotNull(manager.PrecedentsOf(local));
            Assert.IsNotNull(manager.DirectDependantsOf(local));
            Assert.IsNotNull(manager.DirectPrecedentsOf(local));
            Assert.IsNotNull(manager.ResourcesOf(local));
            Assert.IsNotNull(manager.TasksOf(local));
        }

        [Test]
        public void MoveSingleTaskToCheckForOutOfBoundHandling()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");

            // create a task
            var first = new Task();
            manager.Add(first);
            Assert.IsTrue(manager.Tasks.Count() == 1);

            // get task index
            var index = manager.IndexOf(first);
            Assert.IsTrue(index == 0, string.Format("Task index should be {0}, but is {1}", 0, index));

            // move task by 0 offset
            manager.Move(first, 0);
            index = manager.IndexOf(first);
            Assert.IsTrue(index == 0);

            // move task by negative offset
            manager.Move(first, -1);
            index = manager.IndexOf(first);
            Assert.IsTrue(index == 0);

            // move task by count offset 
            manager.Move(first, 1);
            index = manager.IndexOf(first);
            Assert.IsTrue(index == 0);

            // move task by positive offset more than count
            manager.Move(first, 2);
            index = manager.IndexOf(first);
            Assert.IsTrue(index == 0);
        }

        [Test]
        public void MoveNonExistingTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var task = new Task();
            manager.Move(task, 1);
            Assert.IsTrue(manager.IndexOf(task) == -1);
        }

        [Test]
        public void MoveTasksAroundSingleLevel()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");

            // create tasks
            var one = new Task() { Name = "one" };
            var two = new Task() { Name = "one" };
            var three = new Task() { Name = "one" };

            manager.Add(one);
            manager.Add(two);
            manager.Add(three);

            // get index of one
            Assert.IsTrue(manager.IndexOf(one) == 0);
            Assert.IsTrue(manager.IndexOf(two) == 1);
            Assert.IsTrue(manager.IndexOf(three) == 2);
            // move by 1 offset each time
            manager.Move(one, 1);
            Assert.IsTrue(manager.IndexOf(two) == 0, string.Format("{0} != {1}", 0, manager.IndexOf(two)));
            Assert.IsTrue(manager.IndexOf(one) == 1, string.Format("{0} != {1}", 1, manager.IndexOf(one)));
            Assert.IsTrue(manager.IndexOf(three) == 2, string.Format("{0} != {1}", 2, manager.IndexOf(three)));
            manager.Move(one, 1);
            Assert.IsTrue(manager.IndexOf(two) == 0);
            Assert.IsTrue(manager.IndexOf(three) == 1);
            Assert.IsTrue(manager.IndexOf(one) == 2);
            manager.Move(one, 1);
            Assert.IsTrue(manager.IndexOf(two) == 0);
            Assert.IsTrue(manager.IndexOf(three) == 1);
            Assert.IsTrue(manager.IndexOf(one) == 2);
            
            // move by 1 offset each time
            manager.Move(two, 1);
            Assert.IsTrue(manager.IndexOf(three) == 0);
            Assert.IsTrue(manager.IndexOf(two) == 1);
            Assert.IsTrue(manager.IndexOf(one) == 2);
            manager.Move(two, 1);
            Assert.IsTrue(manager.IndexOf(three) == 0);
            Assert.IsTrue(manager.IndexOf(one) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);
            manager.Move(two, 1);
            Assert.IsTrue(manager.IndexOf(three) == 0);
            Assert.IsTrue(manager.IndexOf(one) == 1, string.Format("{0} != {1}", 1, manager.IndexOf(one)));
            Assert.IsTrue(manager.IndexOf(two) == 2, string.Format("{0} != {1}", 2, manager.IndexOf(two)));

            // move by -1 offset each time
            manager.Move(two, -1);
            Assert.IsTrue(manager.IndexOf(three) == 0);
            Assert.IsTrue(manager.IndexOf(two) == 1);
            Assert.IsTrue(manager.IndexOf(one) == 2);
            manager.Move(two, -1);
            Assert.IsTrue(manager.IndexOf(two) == 0);
            Assert.IsTrue(manager.IndexOf(three) == 1);
            Assert.IsTrue(manager.IndexOf(one) == 2);
            manager.Move(two, -1);
            Assert.IsTrue(manager.IndexOf(two) == 0);
            Assert.IsTrue(manager.IndexOf(three) == 1);
            Assert.IsTrue(manager.IndexOf(one) == 2);
        }

        [Test]
        public void MoveTaskIntoGroupUpdatesGroupSchedule()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");

            // create tasks
            var group = new Task() { Name = "group" };
            var one = new Task() { Name = "one" };
            var two = new Task() { Name = "two" };
            var three = new Task() { Name = "three" };

            manager.Add(one);
            manager.Add(two);
            manager.Add(three);
            manager.Add(group);

            manager.Group(group, two);
            manager.Group(group, one);
            manager.SetEnd(three, TimeSpan.FromDays(50));
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(one));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(two));
            Assert.IsTrue(!manager.DirectMembersOf(group).Contains(three));
            Assert.IsTrue(manager.IndexOf(three) == 0);
            Assert.IsTrue(manager.IndexOf(group) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);
            Assert.IsTrue(manager.IndexOf(one) == 3);
            Assert.IsTrue(group.Duration < three.Duration);

            // test: move three into group
            manager.Move(three, 2);
            Assert.IsTrue(manager.IndexOf(group) == 0);
            Assert.IsTrue(manager.IndexOf(three) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);
            Assert.IsTrue(manager.IndexOf(one) == 3);
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(one));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(two));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(three));
            Assert.IsTrue(manager.DirectGroupOf(one) == group);
            Assert.IsTrue(manager.DirectGroupOf(two) == group);
            Assert.IsTrue(manager.DirectGroupOf(three) == group);
            Assert.IsTrue(group.Duration == three.Duration);

            // test: move three out of the group
            manager.Move(three, 3);
            Assert.IsTrue(manager.IndexOf(group) == 0);
            Assert.IsTrue(manager.IndexOf(two) == 1);
            Assert.IsTrue(manager.IndexOf(one) == 2);
            Assert.IsTrue(manager.IndexOf(three) == 3);
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(one));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(two));
            Assert.IsTrue(!manager.DirectMembersOf(group).Contains(three));
            Assert.IsTrue(manager.DirectGroupOf(one) == group);
            Assert.IsTrue(manager.DirectGroupOf(two) == group);
            Assert.IsTrue(manager.DirectGroupOf(three) == null);
            Assert.IsTrue(group.Duration == one.Duration);
            Assert.IsTrue(group.Duration == two.Duration);
        }

        [Test]
        public void MoveGroupsAround()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            // groups
            var group1 = new Task() { Name = "group1" };
            var group2 = new Task() { Name = "group2" };
            var group3 = new Task() { Name = "group3" };
            // group 1 tasks
            var g1t1 = new Task() { Name = "g1t1" };
            var g1t2 = new Task() { Name = "g1t2" };
            var g1t3 = new Task() { Name = "g1t3" };
            // group 2 tasks
            var g2t1 = new Task() { Name = "g2t1" };
            var g2t2 = new Task() { Name = "g2t2" };
            var g2t3 = new Task() { Name = "g2t3" };
            // group 3 tasks
            var g3t1 = new Task() { Name = "g3t1" };
            var g3t2 = new Task() { Name = "g3t2" };
            var g3t3 = new Task() { Name = "g3t3" };

            manager.Add(group1);
            manager.Add(group2);
            manager.Add(group3);
            manager.Add(g1t1);
            manager.Add(g1t2);
            manager.Add(g1t3);
            manager.Add(g2t1);
            manager.Add(g2t2);
            manager.Add(g2t3);
            manager.Add(g3t1);
            manager.Add(g3t2);
            manager.Add(g3t3);

            // make groups
            manager.Group(group1, g1t1);
            manager.Group(group1, g1t2);
            manager.Group(group1, g1t3);
            // make groups
            manager.Group(group2, g2t1);
            manager.Group(group2, g2t2);
            manager.Group(group2, g2t3);
            // make groups
            manager.Group(group3, g3t1);
            manager.Group(group3, g3t2);
            manager.Group(group3, g3t3);

            // confirm parents
            Assert.IsTrue(manager.DirectGroupOf(group1) == null);
            Assert.IsTrue(manager.DirectGroupOf(group2) == null);
            Assert.IsTrue(manager.DirectGroupOf(group3) == null);
            Assert.IsTrue(manager.DirectGroupOf(g1t1) == group1);
            Assert.IsTrue(manager.DirectGroupOf(g1t2) == group1);
            Assert.IsTrue(manager.DirectGroupOf(g1t3) == group1);
            Assert.IsTrue(manager.DirectGroupOf(g2t1) == group2);
            Assert.IsTrue(manager.DirectGroupOf(g2t2) == group2);
            Assert.IsTrue(manager.DirectGroupOf(g2t3) == group2);
            Assert.IsTrue(manager.DirectGroupOf(g3t1) == group3);
            Assert.IsTrue(manager.DirectGroupOf(g3t2) == group3);
            Assert.IsTrue(manager.DirectGroupOf(g3t3) == group3);

            // confirm order
            Assert.IsTrue(manager.IndexOf(group1) == 0);
            Assert.IsTrue(manager.IndexOf(g1t1) == 1);
            Assert.IsTrue(manager.IndexOf(g1t2) == 2);
            Assert.IsTrue(manager.IndexOf(g1t3) == 3);
            Assert.IsTrue(manager.IndexOf(group2) == 4);
            Assert.IsTrue(manager.IndexOf(g2t1) == 5);
            Assert.IsTrue(manager.IndexOf(g2t2) == 6);
            Assert.IsTrue(manager.IndexOf(g2t3) == 7);
            Assert.IsTrue(manager.IndexOf(group3) == 8);
            Assert.IsTrue(manager.IndexOf(g3t1) == 9);
            Assert.IsTrue(manager.IndexOf(g3t2) == 10);
            Assert.IsTrue(manager.IndexOf(g3t3) == 11);

            // move group under itself 1 (not allowed)
            manager.Move(group1, 1);
            Assert.IsTrue(manager.IndexOf(group1) == 0, string.Format("{0} != {1}", 0, manager.IndexOf(group1)));
            Assert.IsTrue(manager.IndexOf(g1t1) == 1);
            Assert.IsTrue(manager.IndexOf(g1t2) == 2);
            Assert.IsTrue(manager.IndexOf(g1t3) == 3);
            Assert.IsTrue(manager.IndexOf(group2) == 4);
            Assert.IsTrue(manager.IndexOf(g2t1) == 5);
            Assert.IsTrue(manager.IndexOf(g2t2) == 6);
            Assert.IsTrue(manager.IndexOf(g2t3) == 7);
            Assert.IsTrue(manager.IndexOf(group3) == 8);
            Assert.IsTrue(manager.IndexOf(g3t1) == 9);
            Assert.IsTrue(manager.IndexOf(g3t2) == 10);
            Assert.IsTrue(manager.IndexOf(g3t3) == 11);

            // move group under itself 2 (not allowed)
            manager.Move(group1, 2);
            Assert.IsTrue(manager.IndexOf(group1) == 0);
            Assert.IsTrue(manager.IndexOf(g1t1) == 1);
            Assert.IsTrue(manager.IndexOf(g1t2) == 2);
            Assert.IsTrue(manager.IndexOf(g1t3) == 3);
            Assert.IsTrue(manager.IndexOf(group2) == 4);
            Assert.IsTrue(manager.IndexOf(g2t1) == 5);
            Assert.IsTrue(manager.IndexOf(g2t2) == 6);
            Assert.IsTrue(manager.IndexOf(g2t3) == 7);
            Assert.IsTrue(manager.IndexOf(group3) == 8);
            Assert.IsTrue(manager.IndexOf(g3t1) == 9);
            Assert.IsTrue(manager.IndexOf(g3t2) == 10);
            Assert.IsTrue(manager.IndexOf(g3t3) == 11);

            // move group under itself 3 (not allowed)
            manager.Move(group1, 3);
            Assert.IsTrue(manager.IndexOf(group1) == 0);
            Assert.IsTrue(manager.IndexOf(g1t1) == 1);
            Assert.IsTrue(manager.IndexOf(g1t2) == 2);
            Assert.IsTrue(manager.IndexOf(g1t3) == 3);
            Assert.IsTrue(manager.IndexOf(group2) == 4);
            Assert.IsTrue(manager.IndexOf(g2t1) == 5);
            Assert.IsTrue(manager.IndexOf(g2t2) == 6);
            Assert.IsTrue(manager.IndexOf(g2t3) == 7);
            Assert.IsTrue(manager.IndexOf(group3) == 8);
            Assert.IsTrue(manager.IndexOf(g3t1) == 9);
            Assert.IsTrue(manager.IndexOf(g3t2) == 10);
            Assert.IsTrue(manager.IndexOf(g3t3) == 11);

            // move group under itself 4 (not allowed)
            manager.Move(group1, 4);
            Assert.IsTrue(manager.IndexOf(group2) == 0);
            Assert.IsTrue(manager.IndexOf(g2t1) == 1);
            Assert.IsTrue(manager.IndexOf(g2t2) == 2);
            Assert.IsTrue(manager.IndexOf(g2t3) == 3);
            Assert.IsTrue(manager.IndexOf(group1) == 4, string.Format("{0} != {1}", 0, manager.IndexOf(group1)));
            Assert.IsTrue(manager.IndexOf(g1t1) == 5, string.Format("{0} != {1}", 0, manager.IndexOf(g1t1)));
            Assert.IsTrue(manager.IndexOf(g1t2) == 6, string.Format("{0} != {1}", 0, manager.IndexOf(g1t2)));
            Assert.IsTrue(manager.IndexOf(g1t3) == 7, string.Format("{0} != {1}", 0, manager.IndexOf(g1t3)));
            Assert.IsTrue(manager.IndexOf(group3) == 8);
            Assert.IsTrue(manager.IndexOf(g3t1) == 9);
            Assert.IsTrue(manager.IndexOf(g3t2) == 10);
            Assert.IsTrue(manager.IndexOf(g3t3) == 11);

            // move group under another group
            manager.Move(group3, -1);
            Assert.IsTrue(manager.IndexOf(group2) == 0);
            Assert.IsTrue(manager.IndexOf(g2t1) == 1);
            Assert.IsTrue(manager.IndexOf(g2t2) == 2);
            Assert.IsTrue(manager.IndexOf(g2t3) == 3);
            Assert.IsTrue(manager.IndexOf(group1) == 4, string.Format("{0} != {1}", 0, manager.IndexOf(group1)));
            Assert.IsTrue(manager.IndexOf(g1t1) == 5, string.Format("{0} != {1}", 0, manager.IndexOf(g1t1)));
            Assert.IsTrue(manager.IndexOf(g1t2) == 6, string.Format("{0} != {1}", 0, manager.IndexOf(g1t2)));
            Assert.IsTrue(manager.IndexOf(group3) == 7);
            Assert.IsTrue(manager.IndexOf(g3t1) == 8);
            Assert.IsTrue(manager.IndexOf(g3t2) == 9);
            Assert.IsTrue(manager.IndexOf(g3t3) == 10);
            Assert.IsTrue(manager.IndexOf(g1t3) == 11, string.Format("{0} != {1}", 0, manager.IndexOf(g1t3)));

            // move group within group
            manager.Move(group3, -1);
            Assert.IsTrue(manager.IndexOf(group2) == 0);
            Assert.IsTrue(manager.IndexOf(g2t1) == 1);
            Assert.IsTrue(manager.IndexOf(g2t2) == 2);
            Assert.IsTrue(manager.IndexOf(g2t3) == 3);
            Assert.IsTrue(manager.IndexOf(group1) == 4, string.Format("{0} != {1}", 0, manager.IndexOf(group1)));
            Assert.IsTrue(manager.IndexOf(g1t1) == 5, string.Format("{0} != {1}", 0, manager.IndexOf(g1t1)));
            Assert.IsTrue(manager.IndexOf(group3) == 6);
            Assert.IsTrue(manager.IndexOf(g3t1) == 7);
            Assert.IsTrue(manager.IndexOf(g3t2) == 8);
            Assert.IsTrue(manager.IndexOf(g3t3) == 9);
            Assert.IsTrue(manager.IndexOf(g1t2) == 10, string.Format("{0} != {1}", 0, manager.IndexOf(g1t2)));
            Assert.IsTrue(manager.IndexOf(g1t3) == 11, string.Format("{0} != {1}", 0, manager.IndexOf(g1t3)));

            // move group out of another group
            manager.Move(group3, -2);
            Assert.IsTrue(manager.IndexOf(group2) == 0);
            Assert.IsTrue(manager.IndexOf(g2t1) == 1);
            Assert.IsTrue(manager.IndexOf(g2t2) == 2);
            Assert.IsTrue(manager.IndexOf(g2t3) == 3);
            Assert.IsTrue(manager.IndexOf(group3) == 4);
            Assert.IsTrue(manager.IndexOf(g3t1) == 5);
            Assert.IsTrue(manager.IndexOf(g3t2) == 6);
            Assert.IsTrue(manager.IndexOf(g3t3) == 7);
            Assert.IsTrue(manager.IndexOf(group1) == 8, string.Format("{0} != {1}", 0, manager.IndexOf(group1)));
            Assert.IsTrue(manager.IndexOf(g1t1) == 9, string.Format("{0} != {1}", 0, manager.IndexOf(g1t1)));
            Assert.IsTrue(manager.IndexOf(g1t2) == 10, string.Format("{0} != {1}", 0, manager.IndexOf(g1t2)));
            Assert.IsTrue(manager.IndexOf(g1t3) == 11, string.Format("{0} != {1}", 0, manager.IndexOf(g1t3)));
        }

        [Test]
        public void MovePartBecomeMoveSplitTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            manager.Add(one);
            manager.Add(two);
            manager.Add(split);

            // setup: create split task
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IndexOf(one) == 0);
            Assert.IsTrue(manager.IndexOf(two) == 1);
            Assert.IsTrue(manager.IndexOf(split) == 2);
            Assert.IsTrue(manager.IndexOf(part1) == -1);
            Assert.IsTrue(manager.IndexOf(part2) == -1);

            // test: move part and expect split task to move instead
            manager.Move(split, -1);
            Assert.IsTrue(manager.IndexOf(one) == 0);
            Assert.IsTrue(manager.IndexOf(split) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);

            // test: move again
            manager.Move(part1, -1);
            Assert.IsTrue(manager.IndexOf(split) == 0);
            Assert.IsTrue(manager.IndexOf(one) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);

            // test: move again (no effect, reached the top)
            manager.Move(part2, -1);
            Assert.IsTrue(manager.IndexOf(split) == 0);
            Assert.IsTrue(manager.IndexOf(one) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);

            // test: move down
            manager.Move(split, 1);
            Assert.IsTrue(manager.IndexOf(one) == 0);
            Assert.IsTrue(manager.IndexOf(split) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);

            // test: move out of bounds
            manager.Move(part1, 2);
            Assert.IsTrue(manager.IndexOf(one) == 0);
            Assert.IsTrue(manager.IndexOf(two) == 1);
            Assert.IsTrue(manager.IndexOf(split) == 2);
        }

        #region groups
        [Test]
        public void GroupSetStartAdjustAllMembers()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var precedent1 = new Task();
            var dependant1 = new Task();
            var split1 = new Task();
            var part1a = new Task();
            var part1b = new Task();

            manager.Add(group1);
            manager.Add(precedent1);
            manager.Add(dependant1);
            manager.Add(split1);
            manager.SetDuration(precedent1, TimeSpan.FromDays(10));
            manager.SetDuration(dependant1, TimeSpan.FromDays(10));
            manager.SetDuration(split1, TimeSpan.FromDays(10));
            manager.Split(split1, part1a, part1b, TimeSpan.FromDays(5));

            manager.Group(group1, precedent1);
            manager.Group(group1, split1);
            manager.Relate(precedent1, dependant1);
            manager.SetStart(dependant1, TimeSpan.FromDays(15));
            Assert.IsTrue(group1.Start == TimeSpan.Zero);
            Assert.IsTrue(group1.Duration == TimeSpan.FromDays(10), "Expected 10, Actual: {0}", group1.Duration);
            Assert.IsTrue(group1.End == TimeSpan.FromDays(10));
            Assert.IsTrue(dependant1.Start == TimeSpan.FromDays(15));
            Assert.IsTrue(dependant1.End == TimeSpan.FromDays(25));
            Assert.IsTrue(manager.DirectPrecedentsOf(dependant1).Contains(precedent1));

            // test: bring forward group1
            manager.SetStart(group1, TimeSpan.FromDays(5));
            Assert.IsTrue(group1.Start == TimeSpan.FromDays(5));
            Assert.IsTrue(group1.Duration == TimeSpan.FromDays(10), "Expected 10, Actual: {0}", group1.Duration);
            Assert.IsTrue(group1.End == TimeSpan.FromDays(15));
            Assert.IsTrue(dependant1.Start == TimeSpan.FromDays(15));
            Assert.IsTrue(dependant1.End == TimeSpan.FromDays(25));
            Assert.IsTrue(manager.DirectPrecedentsOf(dependant1).Contains(precedent1));
            // expect all members moved, precedent is also moved
            Assert.IsTrue(precedent1.Start == TimeSpan.FromDays(5));
            Assert.IsTrue(precedent1.Duration == TimeSpan.FromDays(10), "Expected 10, Actual: {0}", group1.Duration);
            Assert.IsTrue(precedent1.End == TimeSpan.FromDays(15));
            // expect all members moved, split is also moved
            Assert.IsTrue(split1.Start == TimeSpan.FromDays(5));
            Assert.IsTrue(split1.Duration == TimeSpan.FromDays(10), "Expected 10, Actual: {0}", group1.Duration);
            Assert.IsTrue(split1.End == TimeSpan.FromDays(15));

            // test: set group start after dependant1 start, expect dependant 1 to move
            manager.SetStart(group1, TimeSpan.FromDays(10));
            Assert.IsTrue(group1.Start == TimeSpan.FromDays(10));
            Assert.IsTrue(group1.Duration == TimeSpan.FromDays(10), "Expected 10, Actual: {0}", group1.Duration);
            Assert.IsTrue(group1.End == TimeSpan.FromDays(20));
            Assert.IsTrue(dependant1.Start == TimeSpan.FromDays(20));
            Assert.IsTrue(dependant1.End == TimeSpan.FromDays(30));
        }

        [Test]
        public void GroupKnownTasksAndGroups()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            
            var one = new Task();
            var two = new Task();
            var three = new Task();
            var four = new Task();
            var five = new Task();
            var six = new Task();

            manager.Add(one);
            manager.Add(two);
            manager.Add(three);
            manager.Add(four);
            manager.Add(five);
            manager.Add(six);

            // test: groupings
            manager.Group(one, two);
            manager.Group(two, three);
            manager.Group(two, six);
            manager.Group(four, five);
            Assert.IsTrue(manager.IsGroup(one));
            Assert.IsTrue(manager.IsGroup(two));
            Assert.IsTrue(!manager.IsGroup(three));
            Assert.IsTrue(manager.IsGroup(four));
            Assert.IsTrue(!manager.IsGroup(five));
            Assert.IsTrue(!manager.IsGroup(six));

            // test: check task
            Assert.IsTrue(manager.Tasks.Contains(one));
            Assert.IsTrue(manager.Tasks.Contains(two));
            Assert.IsTrue(manager.Tasks.Contains(three));
            Assert.IsTrue(manager.Tasks.Contains(four));
            Assert.IsTrue(manager.Tasks.Contains(five));
            Assert.IsTrue(manager.Tasks.Contains(six));

            // test: check decendants
            Assert.IsTrue(manager.MembersOf(four).Contains(five));
            Assert.IsTrue(manager.MembersOf(one).Contains(two));
            Assert.IsTrue(manager.MembersOf(one).Contains(three));
            Assert.IsTrue(manager.MembersOf(one).Contains(six));
            Assert.IsTrue(manager.MembersOf(one).Count() == 3);

            // test: check ancestors
            Assert.IsTrue(manager.GroupsOf(six).Contains(two));
            Assert.IsTrue(manager.GroupsOf(six).Contains(one));
            Assert.IsTrue(manager.GroupsOf(six).Count() == 2);
        }

        [Test]
        public void GroupUnknownTasksIntoGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group = new Task();
            var one = new Task();

            // setup: make only group added
            manager.Add(group);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(group));

            // test: check that we cannot add unknown tasks to group
            manager.Group(group, one);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(group));
        }

        [Test]
        public void GroupTaskIntoUnknownGroups()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group = new Task();
            var one = new Task();

            // setup: make only one added
            manager.Add(one);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(one));

            // test: check that we cannot add tasks to unknown group
            manager.Group(group, one);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(one));
        }

        [Test]
        public void UngroupUnknownTasksFromGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group = new Task();
            var member = new Task();
            var one = new Task();

            // setup: make only group added
            manager.Add(group);
            manager.Group(group, member);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(group));

            // test: check that we cannot add unknown tasks to group
            manager.Ungroup(group, one);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(group));
        }

        [Test]
        public void UngroupTaskFromUnknownGroups()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            IProjectManager<Task, object> other = new ProjectManager<Task, object>("Testing");
            var group = new Task();
            var member = new Task();
            var one = new Task();
            other.Add(group);
            other.Add(member);
            

            // setup: make only one added and foreign group
            manager.Add(one);
            other.Group(group, member);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(one));

            // test: check that we cannot add tasks to unknown group
            manager.Ungroup(group, one);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(one));
        }

        [Test]
        public void UngroupUnknownGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            IProjectManager<Task, object> other = new ProjectManager<Task, object>("Testing");
            var group = new Task();
            var one = new Task();
            other.Add(group);
            other.Add(one);

            // setup: make a foreign group
            other.Group(group, one);
            Assert.IsTrue(other.IsGroup(group));
            Assert.IsTrue(manager.Tasks.Count() == 0);

            // test: check whether can ungroup unknown group
            manager.Ungroup(group);
            Assert.IsTrue(other.IsGroup(group));
            Assert.IsTrue(manager.Tasks.Count() == 0);
        }

        [Test]
        public void UngroupAGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var group2 = new Task();
            var one =  new Task();
            var two = new Task();
            manager.Add(group1);
            manager.Add(group2);
            manager.Add(one);
            manager.Add(two);

            // setup: group1 contains group2; group2 contains one and two
            manager.Group(group1, group2);
            manager.Group(group2, one);
            manager.Group(group2, two);

            // test: group2 is no longer a group; one and two goes into group 1 (parent of group 2)
            manager.Ungroup(group1);
            Assert.IsTrue(!manager.IsGroup(group1));
            Assert.IsTrue(manager.IsGroup(group2));
            Assert.IsTrue(!manager.IsGroup(one));
            Assert.IsTrue(!manager.IsGroup(two));
            Assert.IsTrue(manager.DirectMembersOf(group1).Count() == 0);
            Assert.IsTrue(manager.DirectMembersOf(group2).Count() == 2);
            Assert.IsTrue(manager.Tasks.Count() == 4);
        }

        [Test]
        public void UngroupNonGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            manager.Add(group1);

            // test: ungroup a task that wasn't a group in the first place (no effect)
            manager.Ungroup(group1);
            Assert.IsTrue(!manager.IsGroup(group1));
        }

        [Test]
        public void UngroupNullGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            manager.Add(one);

            // test: ungroup a null group (no effect, should not throw)
            manager.Ungroup(null);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(one));

            // test: ungroup a null group with task (no effect, should not throw)
            manager.Ungroup(null, one);
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(manager.Tasks.Contains(one));
        }

        [Test]
        public void UngroupNullTaskFromGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var one = new Task();
            manager.Add(group1);
            manager.Add(one);

            // setup: make an actual group first
            manager.Group(group1, one);
            Assert.IsTrue(manager.IsGroup(group1));
            Assert.IsTrue(manager.Tasks.Count() == 2);
            Assert.IsTrue(manager.Tasks.Contains(one));
            Assert.IsTrue(manager.Tasks.Contains(group1));

            // test: ungroup null (no effect)
            manager.Ungroup(group1, null);
            Assert.IsTrue(manager.IsGroup(group1));
            Assert.IsTrue(manager.Tasks.Count() == 2);
            Assert.IsTrue(manager.Tasks.Contains(one));
            Assert.IsTrue(manager.Tasks.Contains(group1));
        }

        [Test]
        public void UngroupPartShouldUngroupSplitTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var group = new Task();
            manager.Add(group);
            manager.Add(split);

            // setup: create a group and split under it
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            manager.Group(group, part1); // intentionall group using part
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(split));
            Assert.IsTrue(!manager.IsMember(part1));
            Assert.IsTrue(!manager.IsMember(part2));

            // test: ungroup part should ungroup split task
            manager.Ungroup(group, part2);
            Assert.IsTrue(!manager.IsGroup(group));
            Assert.IsTrue(!manager.IsMember(split));
            Assert.IsTrue(!manager.IsMember(part1));
            Assert.IsTrue(!manager.IsMember(part2));
        }
        [Test]
        public void AdjustGroupDurationOnUngroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var group = new Task();
            var task = new Task();
            manager.Add(group);
            manager.Add(split);
            manager.Add(task);

            // setup: create a group and split under it
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            manager.Group(group, part1); // intentionall group using part
            manager.Group(group, task);
            manager.SetDuration(part1, TimeSpan.FromDays(30));
            manager.SetDuration(part2, TimeSpan.FromDays(15));
            manager.SetDuration(task, TimeSpan.FromDays(20));
            manager.SetStart(task, TimeSpan.FromDays(50));
            Assert.IsTrue(group.Duration == TimeSpan.FromDays(70));

            // test: ungroup part should ungroup split task
            manager.Ungroup(group, part1);
            Assert.IsTrue(group.Duration == TimeSpan.FromDays(20));
        }

        [Test]
        public void GroupNullTaskIntoGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            manager.Add(group1);

            // test: put task into null group (no effect)
            manager.Group(group1, null);
            Assert.IsTrue(manager.Tasks.Count() == 1);
        }

        [Test]
        public void GroupTaskIntoNullGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            
            var one = new Task();
            manager.Add(one);

            // test: put task into null group (no effect)
            manager.Group(null, one);
            Assert.IsTrue(manager.Tasks.Count() == 1);
        }

        [Test]
        public void GroupIntoSelf()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            
            var group1 = new Task();

            // test: group into self (no effect)
            manager.Group(group1, group1);
            Assert.IsTrue(!manager.IsGroup(group1));
            Assert.IsTrue(manager.DirectGroupOf(group1) == null);
        }

        [Test]
        public void GroupChildIntoSelf()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var one = new Task();
            manager.Add(group1);
            manager.Add(one);

            // setup: group1 contain group2 contain one
            manager.Group(group1, one);

            // test: group into self (no effect)
            manager.Group(one, one);
            Assert.IsTrue(!manager.IsGroup(one));
            Assert.IsTrue(manager.DirectGroupOf(one) == group1);
        }

        [Test]
        public void GroupIntoParent()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var one = new Task();
            manager.Add(group1);
            manager.Add(one);

            // setup: group1 contains one
            manager.Group(group1, one);

            // test: grouping into parent (no effect, since already grouped)
            manager.Group(one, group1);
            Assert.IsTrue(manager.IsGroup(group1));
            Assert.IsTrue(!manager.IsGroup(one));
            Assert.IsTrue(manager.DirectGroupOf(one).Equals(group1));
        }

        [Test]
        public void GroupIntoAnotherGroupWhenAlreadyHaveGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var group2 = new Task();
            var one = new Task();
            manager.Add(group1);
            manager.Add(group2);
            manager.Add(one);

            // setup: group1 contains one
            manager.Group(group1, one);

            // test: group one into group2, leaving group1
            manager.Group(group2, one);
            Assert.IsTrue(!manager.IsGroup(group1), string.Format("{0} != {1}", true, manager.IsGroup(group1)));
            Assert.IsTrue(manager.IsGroup(group2));
            Assert.IsTrue(!manager.IsGroup(one));
            Assert.IsTrue(manager.DirectGroupOf(one).Equals(group2));
            Assert.IsTrue(manager.DirectMembersOf(group1).Count() == 0);
            Assert.IsTrue(manager.DirectMembersOf(group2).Count() == 1);
        }

        [Test]
        public void SubGrouping()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            // create tasks in random order
            var a = new Task();
            var b = new Task();
            var c1 = new Task();
            var c2 = new Task();
            var d1 = new Task();
            var d2 = new Task();
            var d3 = new Task();
            var e1 = new Task();
            var e2 = new Task();
            var e3 = new Task();

            manager.Add(b);
            manager.Add(e1);
            manager.Add(a);
            manager.Add(e2);
            manager.Add(c1);
            manager.Add(d1);
            manager.Add(d2);
            manager.Add(e3);
            manager.Add(d3);
            manager.Add(c2);

            // setup: make sub groups
            manager.Group(a, b);
            manager.Group(b, c1);
            manager.Group(b, c2);
            manager.Group(c1, d1);
            manager.Group(c1, d2);
            manager.Group(c2, d3);
            manager.Group(d1, e1);
            manager.Group(d2, e2);
            manager.Group(d2, e3);

            // test: check sub groups are correct
            Assert.IsTrue(manager.DirectMembersOf(a).Contains(b));
            Assert.IsTrue(manager.DirectMembersOf(b).Contains(c1));
            Assert.IsTrue(manager.DirectMembersOf(b).Contains(c2));
            Assert.IsTrue(manager.DirectMembersOf(c1).Contains(d1));
            Assert.IsTrue(manager.DirectMembersOf(c1).Contains(d2));
            Assert.IsTrue(manager.DirectMembersOf(c2).Contains(d3));
            Assert.IsTrue(manager.DirectMembersOf(d1).Contains(e1));
            Assert.IsTrue(manager.DirectMembersOf(d2).Contains(e2));
            Assert.IsTrue(manager.DirectMembersOf(d2).Contains(e3));
        }

        [Test]
        public void SubGroupOrdering()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            
            // create tasks in random order
            var a = new Task();
            var b = new Task();
            var c1 = new Task();
            var c2 = new Task();
            var d1 = new Task();
            var d2 = new Task();
            var d3 = new Task();
            var e1 = new Task();
            var e2 = new Task();
            var e3 = new Task();

            manager.Add(b);
            manager.Add(e1);
            manager.Add(a);
            manager.Add(e2);
            manager.Add(c1);
            manager.Add(d1);
            manager.Add(d2);
            manager.Add(e3);
            manager.Add(d3);
            manager.Add(c2);

            // setup: make sub groups
            manager.Group(a, b);
            manager.Group(b, c1);
            manager.Group(b, c2);
            manager.Group(c1, d1);
            manager.Group(c1, d2);
            manager.Group(c2, d3);
            manager.Group(d1, e1);
            manager.Group(d2, e2);
            manager.Group(d2, e3);

            // setup: make sub groups
            manager.Group(a, b);
            manager.Group(b, c1);
            manager.Group(b, c2);
            manager.Group(c1, d1);
            manager.Group(c1, d2);
            manager.Group(c2, d3);
            manager.Group(d1, e1);
            manager.Group(d2, e2);
            manager.Group(d2, e3);

            // test: check that order is correct
            Assert.IsTrue(manager.IndexOf(a) == 0, string.Format("Order a = {0} != {1}", manager.IndexOf(a), 0));
            Assert.IsTrue(manager.IndexOf(b) == 1, string.Format("Order a = {0} != {1}", manager.IndexOf(b), 1));
            Assert.IsTrue(manager.IndexOf(c1) == 2, string.Format("Order a = {0} != {1}", manager.IndexOf(c1), 2));
            Assert.IsTrue(manager.IndexOf(d1) == 3, string.Format("Order a = {0} != {1}", manager.IndexOf(d1), 3));
            Assert.IsTrue(manager.IndexOf(e1) == 4, string.Format("Order a = {0} != {1}", manager.IndexOf(d2), 4));
            Assert.IsTrue(manager.IndexOf(d2) == 5, string.Format("Order a = {0} != {1}", manager.IndexOf(d3), 5));
            Assert.IsTrue(manager.IndexOf(e2) == 6, string.Format("Order a = {0} != {1}", manager.IndexOf(e1), 6));
            Assert.IsTrue(manager.IndexOf(e3) == 7, string.Format("Order a = {0} != {1}", manager.IndexOf(e2), 7));
            Assert.IsTrue(manager.IndexOf(c2) == 8, string.Format("Order a = {0} != {1}", manager.IndexOf(e3), 8));
            Assert.IsTrue(manager.IndexOf(d3) == 9, string.Format("Order a = {0} != {1}", manager.IndexOf(a), 9));
        }

        [Test]
        public void GroupIntoGrandChild()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var group2 = new Task();
            var one = new Task();
            manager.Add(group1);
            manager.Add(group2);
            manager.Add(one);

            // setup: group1 contain group2 contain one
            manager.Group(group1, group2);
            manager.Group(group2, one);

            // group into grandchild (no effect)
            manager.Group(one, group1);
            Assert.IsTrue(manager.DirectMembersOf(group1).Contains(group2));
            Assert.IsTrue(manager.DirectMembersOf(group2).Contains(one));
            Assert.IsTrue(manager.DirectMembersOf(one).Count() == 0);
        }

        [Test]
        public void GroupIntoGrandParent()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var group2 = new Task();
            var one = new Task();
            manager.Add(group1);
            manager.Add(group2);
            manager.Add(one);

            // setup: group1 contain group2 contain one
            manager.Group(group1, group2);
            manager.Group(group2, one);

            // test: group into grandparent (allowed)
            manager.Group(group1, one);
            Assert.IsTrue(manager.IsGroup(group1));
            Assert.IsTrue(!manager.IsGroup(group2));
            Assert.IsTrue(!manager.IsGroup(one));
            Assert.IsTrue(manager.DirectGroupOf(one).Equals(group1));
            Assert.IsTrue(manager.DirectGroupOf(group2).Equals(group1));
            Assert.IsTrue(manager.DirectGroupOf(group1) == null);
        }

        [Test]
        public void GroupIntoChild()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group1 = new Task();
            var one = new Task();
            manager.Add(group1);
            manager.Add(one);

            // setup: group1 contains one
            manager.Group(group1, one);

            // test: group into child (no effect)
            manager.Group(one, group1);
            Assert.IsTrue(manager.MembersOf(group1).Contains(one));
            Assert.IsTrue(manager.DirectGroupOf(one).Equals(group1));
            Assert.IsTrue(manager.DirectGroupOf(group1) == null);
        }

        #endregion groups

        [Test]
        public void GroupTaskLevelAndOrdering()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");

            var zero = new Task() { Name = "zero" };
            var one = new Task() { Name = "one" };
            var two = new Task() { Name = "two" };
            var three = new Task() { Name = "three" };
            var four = new Task() { Name = "four" };
            var five = new Task() { Name = "five" };

            manager.Add(zero);
            manager.Add(one);
            manager.Add(two);
            manager.Add(three);
            manager.Add(four);
            manager.Add(five);

            // single level ordering
            Assert.IsTrue(manager.IndexOf(zero) == 0);
            Assert.IsTrue(manager.IndexOf(one) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);
            Assert.IsTrue(manager.IndexOf(three) == 3);
            Assert.IsTrue(manager.IndexOf(four) == 4);
            Assert.IsTrue(manager.IndexOf(five) == 5);
            Assert.IsTrue(manager.Tasks.Count() == 6);

            // two level ordering
            manager.Group(zero, two);
            manager.Group(zero, three);
            Assert.IsTrue(manager.IndexOf(zero) == 0);
            Assert.IsTrue(manager.IndexOf(two) == 1);
            Assert.IsTrue(manager.IndexOf(three) == 2);
            Assert.IsTrue(manager.IndexOf(one) == 3);
            Assert.IsTrue(manager.IndexOf(four) == 4);
            Assert.IsTrue(manager.IndexOf(five) == 5);
            Assert.IsTrue(manager.Tasks.Count() == 6);

            // three level ordering
            manager.Group(five, zero);
            Assert.IsTrue(manager.IndexOf(one) == 0);
            Assert.IsTrue(manager.IndexOf(four) == 1);
            Assert.IsTrue(manager.IndexOf(five) == 2, string.Format("Assert index == {0}; but index == {1}", 0, manager.IndexOf(five)));
            Assert.IsTrue(manager.IndexOf(zero) == 3);
            Assert.IsTrue(manager.IndexOf(two) == 4);
            Assert.IsTrue(manager.IndexOf(three) == 5);
            Assert.IsTrue(manager.Tasks.Count() == 6);

            // twin three level ordering
            manager.Group(four, one);
            Assert.IsTrue(manager.IndexOf(four) == 0);
            Assert.IsTrue(manager.IndexOf(one) == 1);
            Assert.IsTrue(manager.IndexOf(five) == 2, string.Format("Assert index == {0}; but index == {1}", 0, manager.IndexOf(five)));
            Assert.IsTrue(manager.IndexOf(zero) == 3);
            Assert.IsTrue(manager.IndexOf(two) == 4);
            Assert.IsTrue(manager.IndexOf(three) == 5);
            Assert.IsTrue(manager.Tasks.Count() == 6);

            // four level ordering
            manager.Group(two, four);
            Assert.IsTrue(manager.IndexOf(five) == 0, string.Format("Assert index == {0}; but index == {1}", 0, manager.IndexOf(five)));
            Assert.IsTrue(manager.IndexOf(zero) == 1);
            Assert.IsTrue(manager.IndexOf(two) == 2);
            Assert.IsTrue(manager.IndexOf(four) == 3);
            Assert.IsTrue(manager.IndexOf(one) == 4);
            Assert.IsTrue(manager.IndexOf(three) == 5);
            Assert.IsTrue(manager.Tasks.Count() == 6, string.Format("{0} != {1}", 6, manager.Tasks.Count()));

            // check parents
            Assert.IsTrue(manager.DirectGroupOf(zero).Equals(five));
            Assert.IsTrue(manager.DirectGroupOf(one).Equals(four));
            Assert.IsTrue(manager.DirectGroupOf(two).Equals(zero));
            Assert.IsTrue(manager.DirectGroupOf(three).Equals(zero));
            Assert.IsTrue(manager.DirectGroupOf(four).Equals(two));
            Assert.IsTrue(manager.DirectGroupOf(five) == null);
        }

        [Test]
        public void CreateRelation()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            manager.Add(one);
            manager.Add(two);
            
            // setup: confirms no relations
            Assert.IsTrue(manager.DependantsOf(one).Count() == 0);
            Assert.IsTrue(manager.PrecedentsOf(two).Count() == 0);

            // test: create a relationship
            manager.Relate(one, two);
            Assert.IsTrue(manager.DependantsOf(one).Contains(two));
            Assert.IsTrue(manager.PrecedentsOf(two).Contains(one));
            Assert.IsTrue(manager.HasRelations(one));
            Assert.IsTrue(manager.HasRelations(two));
        }
        
        [Test]
        public void CreateRelationWithNull()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            manager.Add(one);

            // test: null precedent (no effect)
            manager.Relate(null, one);
            Assert.IsTrue(manager.HasRelations(one) == false);
            Assert.IsTrue(manager.HasRelations(null) == false);

            // test: null dependant (no effect)
            manager.Relate(one, null);
            Assert.IsTrue(manager.HasRelations(one) == false);
            Assert.IsTrue(manager.HasRelations(null) == false);
        }
        
        [Test]
        public void CreateRelationWithUnknownTasks()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            manager.Add(one);

            // test: non-added dependant (no effect)
            manager.Relate(one, two);
            Assert.IsTrue(manager.HasRelations(one) == false);
            Assert.IsTrue(manager.HasRelations(two) == false);

            // test: non-added precedent (no effect)
            manager.Relate(two, one);
            Assert.IsTrue(manager.HasRelations(one) == false);
            Assert.IsTrue(manager.HasRelations(two) == false);
        }
       
        [Test]
        public void CreateRelationThatAlreadyExist()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            manager.Add(one);
            manager.Add(two);

            // setup: create a relationship
            manager.Relate(one, two);
            Assert.IsTrue(manager.DependantsOf(one).Contains(two));
            Assert.IsTrue(manager.PrecedentsOf(two).Contains(one));

            // test: re-create the relationship (no effect)
            manager.Relate(one, two);
            Assert.IsTrue(manager.DependantsOf(one).Contains(two));
            Assert.IsTrue(manager.PrecedentsOf(two).Contains(one));
        }
        
        [Test]
        public void RemoveExistingRelation()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            manager.Add(one);
            manager.Add(two);

            // setup: create a relationship
            manager.Relate(one, two);
            Assert.IsTrue(manager.DependantsOf(one).Contains(two));
            Assert.IsTrue(manager.PrecedentsOf(two).Contains(one));

            // test: remove relation
            manager.Unrelate(one, two);
            Assert.IsTrue(manager.DependantsOf(one).Count() == 0);
            Assert.IsTrue(manager.PrecedentsOf(two).Count() == 0);
        }
       
        [Test]
        public void RemoveAllDependantsRelation()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var three = new Task();
            var four = new Task();
            manager.Add(one);
            manager.Add(two);
            manager.Add(three);
            manager.Add(four);

            // setup: create a relationship
            manager.Relate(four, one);
            manager.Relate(one, two);
            manager.Relate(one, three);
            Assert.IsTrue(manager.DependantsOf(one).Contains(three));
            Assert.IsTrue(manager.PrecedentsOf(two).Contains(one));
            Assert.IsTrue(manager.PrecedentsOf(three).Contains(one));
            Assert.IsTrue(manager.DependantsOf(four).Contains(one));
            Assert.IsTrue(manager.PrecedentsOf(one).Contains(four));

            // test: remove dependants
            manager.Unrelate(one);
            Assert.IsTrue(manager.DependantsOf(one).Count() == 0);
            Assert.IsTrue(manager.PrecedentsOf(two).Count() == 0);
            Assert.IsTrue(manager.PrecedentsOf(three).Count() == 0);
            Assert.IsTrue(manager.DependantsOf(four).Contains(one));
            Assert.IsTrue(manager.PrecedentsOf(one).Contains(four));
        }
       
        [Test]
        public void RemoveNonExistingRelation()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            manager.Add(one);
            manager.Add(two);

            // setup: confirm current relations
            Assert.IsTrue(manager.HasRelations(one) == false);
            Assert.IsTrue(manager.HasRelations(two) == false);

            // test: check that there is no effect
            manager.Unrelate(one, two);
            Assert.IsTrue(manager.HasRelations(one) == false);
            Assert.IsTrue(manager.HasRelations(two) == false);
        }
        
        [Test]
        public void Create3LevelRelations()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var three = new Task();
            manager.Add(one);
            manager.Add(two);
            manager.Add(three);

            // setup: one before two before three
            manager.Relate(one, two);
            manager.Relate(two, three);

            // test: check enumerations are established
            Assert.IsTrue(manager.DependantsOf(one).Count() == 2);
            Assert.IsTrue(manager.DependantsOf(two).Count() == 1);
            Assert.IsTrue(manager.DependantsOf(three).Count() == 0);

            Assert.IsTrue(manager.DirectDependantsOf(one).Count() == 1);
            Assert.IsTrue(manager.DirectDependantsOf(two).Count() == 1);
            Assert.IsTrue(manager.DirectDependantsOf(three).Count() == 0);

            Assert.IsTrue(manager.PrecedentsOf(one).Count() == 0);
            Assert.IsTrue(manager.PrecedentsOf(two).Count() == 1, string.Format("expected {0} != {1}", 1, manager.PrecedentsOf(two).Count()));
            Assert.IsTrue(manager.PrecedentsOf(three).Count() == 2);

            Assert.IsTrue(manager.DirectPrecedentsOf(one).Count() == 0);
            Assert.IsTrue(manager.DirectPrecedentsOf(two).Count() == 1);
            Assert.IsTrue(manager.DirectPrecedentsOf(three).Count() == 1);
        }
        
        [Test]
        public void CircularRelationLevel1()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            manager.Add(one);
            manager.Add(two);

            // setup: create a relationship
            manager.Relate(one, two);
            Assert.IsTrue(manager.DependantsOf(one).Contains(two));
            Assert.IsTrue(manager.DependantsOf(one).Count() == 1);
            Assert.IsTrue(manager.PrecedentsOf(one).Count() == 0);

            Assert.IsTrue(manager.PrecedentsOf(two).Contains(one));
            Assert.IsTrue(manager.PrecedentsOf(two).Count() == 1);
            Assert.IsTrue(manager.DependantsOf(two).Count() == 0);

            // test: cycle the relationship (no effect)
            manager.Relate(two, one);
            Assert.IsTrue(manager.DependantsOf(one).Contains(two));
            Assert.IsTrue(manager.DependantsOf(one).Count() == 1);
            Assert.IsTrue(manager.PrecedentsOf(one).Count() == 0);

            Assert.IsTrue(manager.PrecedentsOf(two).Contains(one));
            Assert.IsTrue(manager.PrecedentsOf(two).Count() == 1);
            Assert.IsTrue(manager.DependantsOf(two).Count() == 0);
        }
        
        [Test]
        public void CircularRelationLevel2()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var three = new Task();
            manager.Add(one);
            manager.Add(two);
            manager.Add(three);

            // setup: one before two before three;
            manager.Relate(one, two);
            manager.Relate(two, three);
            Assert.IsTrue(manager.HasRelations(one));
            Assert.IsTrue(manager.HasRelations(two));
            Assert.IsTrue(manager.HasRelations(three));
            Assert.IsTrue(manager.DirectDependantsOf(one).Contains(two));
            Assert.IsTrue(manager.DirectDependantsOf(two).Contains(three));
            Assert.IsTrue(manager.PrecedentsOf(three).Contains(one));
            Assert.IsTrue(manager.PrecedentsOf(three).Contains(two));

            // test: prevent circular relation (no effect)
            manager.Relate(three, one);
            Assert.IsTrue(manager.HasRelations(one));
            Assert.IsTrue(manager.HasRelations(two));
            Assert.IsTrue(manager.HasRelations(three));
            Assert.IsTrue(manager.DirectDependantsOf(one).Contains(two));
            Assert.IsTrue(manager.DirectDependantsOf(two).Contains(three));
            Assert.IsTrue(manager.PrecedentsOf(three).Contains(one));
            Assert.IsTrue(manager.PrecedentsOf(three).Contains(two));
        }
        
        [Test]
        public void RelateMultipleDependants()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var three = new Task();
            manager.Add(one);
            manager.Add(two);
            manager.Add(three);

            // setup: confirm no relations exists
            Assert.IsTrue(!manager.HasRelations(one));
            Assert.IsTrue(!manager.HasRelations(two));
            Assert.IsTrue(!manager.HasRelations(three));
            
            // test: multiple relation setup
            manager.Relate(one, two);
            manager.Relate(one, three);
            Assert.IsTrue(manager.HasRelations(one));
            Assert.IsTrue(manager.HasRelations(two));
            Assert.IsTrue(manager.HasRelations(three));
            Assert.IsTrue(manager.DirectDependantsOf(one).Count() == 2);
            Assert.IsTrue(manager.DirectPrecedentsOf(two).Count() == 1);
            Assert.IsTrue(manager.DirectPrecedentsOf(three).Count() == 1);
        }
        
        [Test]
        public void GroupCollapseDoesNotAffectRelateSplitTask()
        {
            var manager = new ProjectManager<Task, object>("Testing");
            var precedent = new Task() { Name = "Precedent" };
            var split = new Task() { Name = "split" };
            var part1 = new Task() { Name = "part1" };
            var part2 = new Task() { Name = "part2" };
            var dependant = new Task() { Name = "dependant" };
            var group = new Task() { Name = "group" };

            manager.Add(precedent);
            manager.Add(dependant);
            manager.Add(split);
            manager.Add(group);

            manager.Group(group, precedent);
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            manager.Group(group, part1);
            Assert.IsTrue(!manager.HasRelations(precedent));
            Assert.IsTrue(!manager.HasRelations(dependant));
            Assert.IsTrue(!manager.HasRelations(split));
            Assert.IsTrue(manager.DependantsOf(precedent).Count() == 0);
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Count() == 0);
            Assert.IsTrue(manager.Precedents.Count() == 0);
            Assert.IsTrue(manager.GroupsOf(precedent).Count() == 1);
            Assert.IsTrue(manager.MembersOf(group).Count() == 2);

            // test: relate tasks
            manager.Relate(precedent, part1);
            manager.Relate(precedent, dependant);
            Assert.IsTrue(manager.HasRelations(precedent), "Expect precedent has relations");
            Assert.IsTrue(manager.HasRelations(dependant), "Expect precedent has relations");
            Assert.IsTrue(manager.HasRelations(split), "Expect precedent has relations");
            Assert.IsTrue(manager.DependantsOf(precedent).Count() == 2, "Expect precedent has 2 dependants");
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Count() == 2, "Expect precedent has 2 direct dependants");
            Assert.IsTrue(manager.Precedents.Count() == 1);
            Assert.IsTrue(manager.GroupsOf(precedent).Count() == 1);
            Assert.IsTrue(manager.GroupsOf(split).Count() == 1);
            Assert.IsTrue(manager.MembersOf(group).Count() == 2);
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Contains(dependant));
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Contains(split));

            // test: collapse group
            manager.SetCollapse(group, true);
            Assert.IsTrue(manager.HasRelations(precedent), "Expect precedent has relations");
            Assert.IsTrue(manager.HasRelations(dependant), "Expect precedent has relations");
            Assert.IsTrue(manager.HasRelations(split), "Expect precedent has relations");
            Assert.IsTrue(manager.DependantsOf(precedent).Count() == 2, "Expect precedent has 2 dependants");
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Count() == 2, "Expect precedent has 2 direct dependants");
            Assert.IsTrue(manager.Precedents.Count() == 1);
            Assert.IsTrue(manager.GroupsOf(precedent).Count() == 1);
            Assert.IsTrue(manager.GroupsOf(split).Count() == 1);
            Assert.IsTrue(manager.MembersOf(group).Count() == 2);
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Contains(dependant));
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Contains(split));

            // test: uncollapse group
            manager.SetCollapse(group, false);
            Assert.IsTrue(manager.HasRelations(precedent), "Expect precedent has relations");
            Assert.IsTrue(manager.HasRelations(dependant), "Expect precedent has relations");
            Assert.IsTrue(manager.HasRelations(split), "Expect precedent has relations");
            Assert.IsTrue(manager.DependantsOf(precedent).Count() == 2, "Expect precedent has 2 dependants");
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Count() == 2, "Expect precedent has 2 direct dependants");
            Assert.IsTrue(manager.Precedents.Count() == 1);
            Assert.IsTrue(manager.GroupsOf(precedent).Count() == 1);
            Assert.IsTrue(manager.GroupsOf(split).Count() == 1);
            Assert.IsTrue(manager.MembersOf(group).Count() == 2);
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Contains(dependant));
            Assert.IsTrue(manager.DirectDependantsOf(precedent).Contains(split));
        }

        [Test]
        public void AssignResource()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var res = new Task() { Name = "Resource" };
            manager.Add(one);

            // setup: confirm there is no resource
            Assert.IsTrue(manager.Resources.Count() == 0);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 0);
            Assert.IsTrue(manager.TasksOf(res).Count() == 0);

            // test: assign resource
            manager.Assign(one, res);
            Assert.IsTrue(manager.Resources.Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 1);
            Assert.IsTrue(manager.TasksOf(res).Count() == 1);
            Assert.IsTrue(manager.Resources.Contains(res));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(res));
            Assert.IsTrue(manager.TasksOf(res).Contains(one));
        }
        
        [Test]
        public void AssignResourceForUnknownTasks()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var res = new Task() { Name = "Resource" };

            // setup: confirm there is no resource
            Assert.IsTrue(manager.Resources.Count() == 0);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 0);
            Assert.IsTrue(manager.TasksOf(res).Count() == 0);

            // test: assign resource
            manager.Assign(one, res);
            Assert.IsTrue(manager.Resources.Count() == 0);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 0);
            Assert.IsTrue(manager.TasksOf(res).Count() == 0);
        }

        [Test]
        public void AssignSameResourceToSameTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var res = new Task() { Name = "Resource" };
            manager.Add(one);

            // setup: assign resource
            manager.Assign(one, res);
            Assert.IsTrue(manager.Resources.Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 1);
            Assert.IsTrue(manager.TasksOf(res).Count() == 1);
            Assert.IsTrue(manager.Resources.Contains(res));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(res));
            Assert.IsTrue(manager.TasksOf(res).Contains(one));

            // test: assign the same resource to the same task (no effect, as resource is already assigned)
            manager.Assign(one, res);
            Assert.IsTrue(manager.Resources.Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 1);
            Assert.IsTrue(manager.TasksOf(res).Count() == 1);
            Assert.IsTrue(manager.Resources.Contains(res));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(res));
            Assert.IsTrue(manager.TasksOf(res).Contains(one));
        }

        [Test]
        public void AssignSameResourceToDifferentTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var res = new Task() { Name = "Resource" };
            manager.Add(one);
            manager.Add(two);

            // setup: assign resource
            manager.Assign(one, res);
            Assert.IsTrue(manager.Resources.Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 1);
            Assert.IsTrue(manager.TasksOf(res).Count() == 1);
            Assert.IsTrue(manager.Resources.Contains(res));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(res));
            Assert.IsTrue(manager.TasksOf(res).Contains(one));

            // setup: assign resource. no extra resource should be created. resource reference shared by 2 tasks
            manager.Assign(two, res);
            Assert.IsTrue(manager.Resources.Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(two).Count() == 1);
            Assert.IsTrue(manager.TasksOf(res).Count() == 2);
            Assert.IsTrue(manager.Resources.Contains(res));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(res));
            Assert.IsTrue(manager.ResourcesOf(two).Contains(res));
            Assert.IsTrue(manager.TasksOf(res).Contains(one));
            Assert.IsTrue(manager.TasksOf(res).Contains(two));
        }

        [Test]
        public void TwoWayResourceLookup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var r1 = new Task();
            var r2 = new Task();
            manager.Add(one);
            manager.Add(two);

            // check initializations are correct
            Assert.IsTrue(manager.ResourcesOf(one) != null);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 0);
            Assert.IsTrue(manager.ResourcesOf(two) != null);
            Assert.IsTrue(manager.ResourcesOf(two).Count() == 0);

            // assign resource
            manager.Assign(one, r1);
            Assert.IsTrue(manager.ResourcesOf(one) != null);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(one).FirstOrDefault().Equals(r1));
            Assert.IsTrue(manager.TasksOf(r1).Count() == 1);
            Assert.IsTrue(manager.TasksOf(r1).FirstOrDefault().Equals(one));

            // assign another resource
            manager.Assign(one, r2);
            Assert.IsTrue(manager.ResourcesOf(one) != null);
            Assert.IsTrue(manager.ResourcesOf(one).Count() == 2);
            Assert.IsTrue(manager.ResourcesOf(one).ElementAtOrDefault(1).Equals(r2));
            Assert.IsTrue(manager.TasksOf(r2).Count() == 1);
            Assert.IsTrue(manager.TasksOf(r2).FirstOrDefault().Equals(one));

            // assign resource to another task
            manager.Assign(two, r2);
            Assert.IsTrue(manager.ResourcesOf(two) != null);
            Assert.IsTrue(manager.ResourcesOf(two).Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(two).FirstOrDefault().Equals(r2));
            Assert.IsTrue(manager.TasksOf(r2).Count() == 2);
            Assert.IsTrue(manager.TasksOf(r2).ElementAtOrDefault(1).Equals(two));
        }

        [Test]
        public void UnassignSpecificResource()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var r1 = new Task();
            var r2 = new Task();
            manager.Add(one);

            // setup: assign some resource
            manager.Assign(one, r1);
            manager.Assign(one, r2);
            Assert.IsTrue(manager.Resources.Count() == 2);
            Assert.IsTrue(manager.ResourcesOf(one).Contains(r1));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(r2));
            Assert.IsTrue(manager.TasksOf(r1).Contains(one));
            Assert.IsTrue(manager.TasksOf(r2).Contains(one));

            // test: unassign resource
            manager.Unassign(one, r2);
            Assert.IsTrue(manager.Resources.Count() == 1);
            Assert.IsTrue(manager.ResourcesOf(one).Contains(r1));
            Assert.IsTrue(!manager.ResourcesOf(one).Contains(r2));
            Assert.IsTrue(manager.TasksOf(r1).Contains(one));
            Assert.IsTrue(!manager.TasksOf(r2).Contains(one));
        }

        [Test]
        public void UnassignAllResourceFromTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var r1 = new Task();
            var r2 = new Task();
            manager.Add(one);
            manager.Add(two);

            // setup: assign some resource
            manager.Assign(one, r1);
            manager.Assign(one, r2);
            manager.Assign(two, r1);
            manager.Assign(two, r2);
            Assert.IsTrue(manager.Resources.Count() == 2);
            Assert.IsTrue(manager.ResourcesOf(one).Contains(r1));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(r2));
            Assert.IsTrue(manager.ResourcesOf(two).Contains(r1));
            Assert.IsTrue(manager.ResourcesOf(two).Contains(r2));
            Assert.IsTrue(manager.TasksOf(r1).Contains(one));
            Assert.IsTrue(manager.TasksOf(r2).Contains(one));
            Assert.IsTrue(manager.TasksOf(r1).Contains(two));
            Assert.IsTrue(manager.TasksOf(r2).Contains(two));

            manager.Unassign(one);
            Assert.IsTrue(manager.Resources.Count() == 2);
            Assert.IsTrue(!manager.ResourcesOf(one).Contains(r1));
            Assert.IsTrue(!manager.ResourcesOf(one).Contains(r2));
            Assert.IsTrue(manager.ResourcesOf(two).Contains(r1));
            Assert.IsTrue(manager.ResourcesOf(two).Contains(r2));
            Assert.IsTrue(!manager.TasksOf(r1).Contains(one));
            Assert.IsTrue(!manager.TasksOf(r2).Contains(one));
            Assert.IsTrue(manager.TasksOf(r1).Contains(two));
            Assert.IsTrue(manager.TasksOf(r2).Contains(two));
        }

        [Test]
        public void UnassignResourceFromAllTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var r1 = new object();
            var r2 = new object();
            manager.Add(one);
            manager.Add(two);

            // setup: assign some resource
            manager.Assign(one, r1);
            manager.Assign(one, r2);
            manager.Assign(two, r1);
            manager.Assign(two, r2);
            Assert.IsTrue(manager.Resources.Count() == 2);
            Assert.IsTrue(manager.ResourcesOf(one).Contains(r1));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(r2));
            Assert.IsTrue(manager.ResourcesOf(two).Contains(r1));
            Assert.IsTrue(manager.ResourcesOf(two).Contains(r2));
            Assert.IsTrue(manager.TasksOf(r1).Contains(one));
            Assert.IsTrue(manager.TasksOf(r2).Contains(one));
            Assert.IsTrue(manager.TasksOf(r1).Contains(two));
            Assert.IsTrue(manager.TasksOf(r2).Contains(two));

            manager.Unassign(r1);
            Assert.IsTrue(manager.Resources.Count() == 1, string.Format("Expected {0} != {1}", 1, manager.Resources.Count()));
            Assert.IsTrue(!manager.ResourcesOf(one).Contains(r1));
            Assert.IsTrue(manager.ResourcesOf(one).Contains(r2));
            Assert.IsTrue(!manager.ResourcesOf(two).Contains(r1));
            Assert.IsTrue(manager.ResourcesOf(two).Contains(r2));
            Assert.IsTrue(!manager.TasksOf(r1).Contains(one));
            Assert.IsTrue(manager.TasksOf(r2).Contains(one));
            Assert.IsTrue(!manager.TasksOf(r1).Contains(two));
            Assert.IsTrue(manager.TasksOf(r2).Contains(two));
        }
       
        [Test]
        public void GroupCannotBeRelated()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group = new Task();
            var member = new Task();
            var one = new Task();
            manager.Add(group);
            manager.Add(member);
            manager.Add(one);

            // setup: make a group
            manager.Group(group, member);
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(member));
            Assert.IsTrue(!manager.HasRelations(one));

            // test: relate a task and a group (not allowed)
            manager.Relate(group, one);
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(member));
            Assert.IsTrue(!manager.HasRelations(group));
            Assert.IsTrue(!manager.HasRelations(one));

            // test: relate a task and a group, now in another order (not allowed)
            manager.Relate(one, group);
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(member));
            Assert.IsTrue(!manager.HasRelations(group));
            Assert.IsTrue(!manager.HasRelations(one));
        }
        
        [Test]
        public void HasRelationTaskCannotBecomeGroup()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var member = new Task();
            manager.Add(one);
            manager.Add(two);
            manager.Add(member);

            // setup: create a relation
            manager.Relate(one, two);
            Assert.IsTrue(manager.HasRelations(one));
            Assert.IsTrue(manager.HasRelations(two));

            // test: make a group with another task (not allowed)
            manager.Group(one, member);
            Assert.IsTrue(manager.HasRelations(one));
            Assert.IsTrue(manager.HasRelations(two));
            Assert.IsTrue(!manager.IsGroup(one));
            Assert.IsTrue(!manager.IsMember(member));

            // test: make a group with another task (not allowed)
            manager.Group(two, member);
            Assert.IsTrue(manager.HasRelations(one));
            Assert.IsTrue(manager.HasRelations(two));
            Assert.IsTrue(!manager.IsGroup(two));
            Assert.IsTrue(!manager.IsMember(member));
        }

        [Test]
        public void SplitTaskAndConfirmStructure()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            manager.Add(split);

            // setup: confirm task type status
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsSplit(part1));
            Assert.IsTrue(!manager.IsSplit(part2));
            Assert.IsTrue(!manager.IsPart(split));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));

            // test: split the task into part1 and part2 and see resulting structure
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));

            Assert.IsTrue(!manager.IsPart(split));
            Assert.IsTrue(!manager.IsSplit(part1));
            Assert.IsTrue(!manager.IsSplit(part2));

            Assert.IsTrue(manager.PartsOf(split).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split).Contains(part1));
            Assert.IsTrue(manager.PartsOf(split).Contains(part2));
        }

        [Test]
        public void SplitTaskAndConfirmSchedule()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task() { Name = "split" };
            var part1 = new Task() { Name = "part1" };
            var part2 = new Task() { Name = "part2" };
            manager.Add(split);

            // setup: set schedule on regular task
            manager.SetStart(split, TimeSpan.FromDays(11));
            manager.SetDuration(split, TimeSpan.FromDays(15));
            Assert.IsTrue(split.Start == TimeSpan.FromDays(11));
            Assert.IsTrue(split.End == TimeSpan.FromDays(26));

            // test: split the task into parts and make sure the schedules of the new parts tally with original task
            manager.Split(split, part1, part2, TimeSpan.FromDays(5));
            Assert.IsTrue(part1.Duration == TimeSpan.FromDays(5));
            Assert.IsTrue(part1.Start == TimeSpan.FromDays(11));
            Assert.IsTrue(part1.End == TimeSpan.FromDays(16));

            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(10));
            Assert.IsTrue(part2.Start == TimeSpan.FromDays(16));
            Assert.IsTrue(part2.End == TimeSpan.FromDays(26));
            
            Assert.IsTrue(split.Start == TimeSpan.FromDays(11));
            Assert.IsTrue(split.End == TimeSpan.FromDays(26));
            Assert.IsTrue(split.Duration == TimeSpan.FromDays(15));
        }

        [Test]
        public void SplitNullTaskNoEffect()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var part1 = new Task();
            var part2 = new Task();

            // setup: confirm part is not part
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));

            // test: try to split a null task
            manager.Split(null, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
        }

        [Test]
        public void SplitTaskUsingRegularTaskNoEffect()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var one = new Task();
            var two = new Task();
            var three = new Task();
            manager.Add(one);
            manager.Add(two);
            manager.Add(three);

            // setup: confirm we have 3 regular tasks
            Assert.IsTrue(manager.Tasks.Count() == 3);

            // test: split one using two and three
            manager.Split(one, two, three, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.Tasks.Count() == 3);
            Assert.IsTrue(!manager.IsSplit(one));
            Assert.IsTrue(!manager.IsPart(two));
            Assert.IsTrue(!manager.IsPart(two));
        }

        [Test]
        public void SplitTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            manager.Add(split);

            // setup: confirm no split tasks
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));

            // test: split the task
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.PartsOf(split).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0).Equals(part1));
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1).Equals(part2));
        }

        [Test]
        public void SplitNonExistingPart()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var part1 = new Task();
            var part2 = new Task();

            // setup: confirm no parts
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
            Assert.IsTrue(manager.Tasks.Count() == 0);

            // test: attempt to split part (no effect)
            manager.Split(part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
            Assert.IsTrue(manager.Tasks.Count() == 0);
        }

        [Test]
        public void SplitNullPart()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            manager.Add(split);

            // setup: create a split task
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(!manager.IsPart(part3));
            
            // test: split a null part (no effect);
            manager.Split(null, part3, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(!manager.IsPart(part3));

            // test: split a null part (no effect);
            manager.Split(part1, null, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(!manager.IsPart(part3));
        }

        [Test]
        public void JoinNullPart()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var task = new Task();
            
            // test: two nulls (no effect)
            manager.Join(null, null);
            Assert.IsTrue(manager.Tasks.Count() == 0);
            
            // test: null existing
            manager.Join(null, task);
            Assert.IsTrue(!manager.IsPart(task));
        }

        [Test]
        public void JoinPartFromDifferentSplitTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split1 = new Task();
            var part1a = new Task();
            var part1b = new Task();
            var split2 = new Task();
            var part2a = new Task();
            var part2b = new Task();
            manager.Add(split1);
            manager.Add(split2);

            // setup: create 2 split tasks
            manager.Split(split1, part1a, part1b, TimeSpan.FromDays(1));
            manager.Split(split2, part2a, part2b, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split1));
            Assert.IsTrue(manager.IsSplit(split2));
            Assert.IsTrue(manager.IsPart(part1a));
            Assert.IsTrue(manager.IsPart(part1b));
            Assert.IsTrue(manager.IsPart(part2a));
            Assert.IsTrue(manager.IsPart(part2b));
            Assert.IsTrue(manager.PartsOf(split1).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split1).Contains(part1a));
            Assert.IsTrue(manager.PartsOf(split1).Contains(part1b));
            Assert.IsTrue(manager.PartsOf(split2).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split2).Contains(part2a));
            Assert.IsTrue(manager.PartsOf(split2).Contains(part2b));

            // test: join part1x with part2x (no effect)
            manager.Join(part1a, part2a);
            Assert.IsTrue(manager.IsSplit(split1));
            Assert.IsTrue(manager.IsSplit(split2));
            Assert.IsTrue(manager.IsPart(part1a));
            Assert.IsTrue(manager.IsPart(part1b));
            Assert.IsTrue(manager.IsPart(part2a));
            Assert.IsTrue(manager.IsPart(part2b));
            Assert.IsTrue(manager.PartsOf(split1).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split1).Contains(part1a));
            Assert.IsTrue(manager.PartsOf(split1).Contains(part1b));
            Assert.IsTrue(manager.PartsOf(split2).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split2).Contains(part2a));
            Assert.IsTrue(manager.PartsOf(split2).Contains(part2b));
        }

        [Test]
        public void JoinPartsConfirmPackedSchedule()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            var part4 = new Task();
            manager.Add(split);

            // setup: create a 4 part split task
            manager.SetDuration(split, TimeSpan.FromDays(40));
            manager.Split(split, part1, part2, TimeSpan.FromDays(5));
            manager.Split(part2, part3, TimeSpan.FromDays(10));
            manager.Split(part3, part4, TimeSpan.FromDays(20));
            Assert.IsTrue(part4.Duration == TimeSpan.FromDays(5));
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(10));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(manager.IsPart(part4));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.PartsOf(split).Count() == 4);

            Assert.IsTrue(part1.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(part1.End == TimeSpan.FromDays(5));
            Assert.IsTrue(part2.Start == TimeSpan.FromDays(5));
            Assert.IsTrue(part2.End == TimeSpan.FromDays(15));
            Assert.IsTrue(part3.Start == TimeSpan.FromDays(15));
            Assert.IsTrue(part3.End == TimeSpan.FromDays(35));
            Assert.IsTrue(part4.Start == TimeSpan.FromDays(35));
            Assert.IsTrue(part4.End == TimeSpan.FromDays(40));

            // test: join part 2 and 4
            manager.Join(part2, part4); // expect part 4 to join into part 2, and part 3 start date gets pushed to later date by the combined duration of 2 and 4
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(15));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(!manager.IsPart(part4));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.PartsOf(split).Count() == 3);

            Assert.IsTrue(part1.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(part1.End == TimeSpan.FromDays(5));
            Assert.IsTrue(part2.Start == TimeSpan.FromDays(5));
            Assert.IsTrue(part2.End == TimeSpan.FromDays(20));
            Assert.IsTrue(part3.Start == TimeSpan.FromDays(20));
            Assert.IsTrue(part3.End == TimeSpan.FromDays(40));
        }

        [Test]
        public void AllowRelateSplitTaskToTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1a = new Task();
            var part1b = new Task();
            var task = new Task();
            manager.Add(split);
            manager.Add(task);

            // setup: create a split task
            manager.Split(split, part1a, part1b, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(!manager.IsSplit(part1a));
            Assert.IsTrue(!manager.IsSplit(part1b));
            Assert.IsTrue(!manager.IsPart(split));
            Assert.IsTrue(manager.IsPart(part1a));
            Assert.IsTrue(manager.IsPart(part1b));
            Assert.IsTrue(!manager.HasRelations(split));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));
            Assert.IsTrue(!manager.HasRelations(task));

            // test: relate task and split task
            manager.Relate(task, split);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(!manager.IsSplit(part1a));
            Assert.IsTrue(!manager.IsSplit(part1b));
            Assert.IsTrue(!manager.IsPart(split));
            Assert.IsTrue(manager.IsPart(part1a));
            Assert.IsTrue(manager.IsPart(part1b));
            Assert.IsTrue(manager.HasRelations(split));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));
            Assert.IsTrue(manager.HasRelations(task));
            Assert.IsTrue(manager.PrecedentsOf(split).Contains(task));
        }

        [Test]
        public void DoNotRelatePartsOfTheSameSplitTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1a = new Task();
            var part1b = new Task();
            manager.Add(split);

            // setup: create a split task
            manager.Split(split, part1a, part1b, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(!manager.IsSplit(part1a));
            Assert.IsTrue(!manager.IsSplit(part1b));
            Assert.IsTrue(!manager.IsPart(split));
            Assert.IsTrue(manager.IsPart(part1a));
            Assert.IsTrue(manager.IsPart(part1b));
            Assert.IsTrue(!manager.HasRelations(split));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));

            // test: relate the two parts
            manager.Relate(part1a, part1b);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(!manager.IsSplit(part1a));
            Assert.IsTrue(!manager.IsSplit(part1b));
            Assert.IsTrue(!manager.IsPart(split));
            Assert.IsTrue(manager.IsPart(part1a));
            Assert.IsTrue(manager.IsPart(part1b));
            Assert.IsTrue(!manager.HasRelations(split));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));
        }

        [Test]
        public void RelateUnrelatePartsBecomeRelateUnrelateSplitTasks()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split1 = new Task();
            var part1a = new Task();
            var part1b = new Task();

            var split2 = new Task();
            var part2a = new Task();
            var part2b = new Task();
            manager.Add(split1);
            manager.Add(split2);

            // setup: create 2 split tasks each of 2 parts
            manager.Split(split1, part1a, part1b, TimeSpan.FromDays(1));
            manager.Split(split2, part2a, part2b, TimeSpan.FromDays(1));
            Assert.IsTrue(!manager.HasRelations(split1));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));
            Assert.IsTrue(!manager.HasRelations(split2));
            Assert.IsTrue(!manager.HasRelations(part2a));
            Assert.IsTrue(!manager.HasRelations(part2b));

            // test: relate parts from different splits
            manager.Relate(part1a, part2a);
            Assert.IsTrue(manager.HasRelations(split1));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));
            Assert.IsTrue(manager.HasRelations(split2));
            Assert.IsTrue(!manager.HasRelations(part2a));
            Assert.IsTrue(!manager.HasRelations(part2b));

            // test: relate parts from different splits
            manager.Unrelate(part1b, part2b);
            Assert.IsTrue(!manager.HasRelations(split1));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));
            Assert.IsTrue(!manager.HasRelations(split2));
            Assert.IsTrue(!manager.HasRelations(part2a));
            Assert.IsTrue(!manager.HasRelations(part2b));

            // test: relate parts using different parts
            manager.Relate(part2b, part1b);
            Assert.IsTrue(manager.HasRelations(split1));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));
            Assert.IsTrue(manager.HasRelations(split2));
            Assert.IsTrue(!manager.HasRelations(part2a));
            Assert.IsTrue(!manager.HasRelations(part2b));

            // test: unrelate all
            manager.Unrelate(part2a);
            Assert.IsTrue(!manager.HasRelations(split1));
            Assert.IsTrue(!manager.HasRelations(part1a));
            Assert.IsTrue(!manager.HasRelations(part1b));
            Assert.IsTrue(!manager.HasRelations(split2));
            Assert.IsTrue(!manager.HasRelations(part2a));
            Assert.IsTrue(!manager.HasRelations(part2b));
        }

        [Test]
        public void SplitTaskUsingSamePartsNoEffect()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part = new Task();
            manager.Add(split);

            // setup: confirm no splits and parts
            Assert.IsTrue(!manager.IsPart(split));
            Assert.IsTrue(!manager.IsPart(part));
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part));

            // test: split using part and part
            manager.Split(split, part, part, TimeSpan.FromDays(1));
            Assert.IsTrue(!manager.IsPart(split));
            Assert.IsTrue(!manager.IsPart(part));
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part));
        }

        [Test]
        public void SplitPartUsingSamePartsNoEffect()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            manager.Add(split);

            // setup: create a split
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.PartsOf(split).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0).Equals(part1));
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1).Equals(part2));

            // test: split part1 using part1
            manager.Split(part1, part1, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.PartsOf(split).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0).Equals(part1));
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1).Equals(part2));
        }

        [Test]
        public void RelatedTaskCannotBecomeParts()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            manager.Add(split);
            manager.Add(part1);
            manager.Add(part2);

            // setup: relate part1 and part2
            manager.Relate(part1, part2);
            Assert.IsTrue(manager.HasRelations(part1));
            Assert.IsTrue(manager.HasRelations(part2));
            Assert.IsTrue(manager.DependantsOf(part1).Contains(part2));

            // test: split task using part1 and part2
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.HasRelations(part1));
            Assert.IsTrue(manager.HasRelations(part2));
            Assert.IsTrue(manager.DependantsOf(part1).Contains(part2));
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
        }

        [Test]
        public void SplitPart()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task() { Name = "part1" };
            var part2 = new Task() { Name = "part2" };
            var part3 = new Task() { Name = "part3" };
            var part4 = new Task() { Name = "part4" };
            manager.Add(split);

            // setup: set up a split task
            manager.Split(split, part1, part3, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(!manager.IsPart(part4));
            Assert.IsTrue(manager.PartsOf(split).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0).Equals(part1));
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1).Equals(part3));
            Assert.IsTrue(split.Start == part1.Start);
            Assert.IsTrue(split.End == part3.End);

            //// test: split part3 to give part3 and part4
            manager.Split(part3, part4, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(manager.IsPart(part4));
            Assert.IsTrue(manager.PartsOf(split).Count() == 3);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0).Equals(part1));
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1).Equals(part3));
            Assert.IsTrue(manager.PartsOf(split).ElementAt(2).Equals(part4));
            Assert.IsTrue(split.Start == part1.Start);
            Assert.IsTrue(split.End == part4.End);

            //// test: split part1 to give part1 and part2
            manager.Split(part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(manager.IsPart(part4));
            Assert.IsTrue(manager.PartsOf(split).Count() == 4);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0) == part1);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1) == part2);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(2) == part3);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(3) == part4);
            Assert.IsTrue(split.Start == part1.Start);
            Assert.IsTrue(split.End == part4.End);

            //// test: ensure no parts overlap
            Assert.IsTrue(part1.End == part2.Start);
            Assert.IsTrue(part2.End == part3.Start);
            Assert.IsTrue(part3.End == part4.Start);
            Assert.IsTrue(part1.Start < part1.End);
            Assert.IsTrue(part4.Start < part4.End);
        }

        [Test]
        public void SplitPartAsThoughItIsTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task() { Name = "part1" };
            var part2 = new Task() { Name = "part2" };
            var part3 = new Task();
            var part4 = new Task();
            manager.Add(split);

            // setup: create a split task with parts
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(!manager.IsSplit(part1));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));

            // test: split the part using the split task method
            manager.Split(part1, part3, part4, TimeSpan.FromDays(1));
            Assert.IsTrue(!manager.IsSplit(part1));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(!manager.IsPart(part3));
            Assert.IsTrue(!manager.IsPart(part4));
        }

        [Test]
        public void JoinParts()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            manager.Add(split);

            // setup: create a split task with 3 parts
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            manager.Split(part2, part3, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(manager.PartsOf(split).Count() == 3);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0) == part1);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1) == part2);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(2) == part3);

            // test: join part 1 and part2
            manager.Join(part1, part2);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(manager.PartsOf(split).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0) == part1);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1) == part3);
        }

        [Test]
        public void JoinPartRemainSinglePartBecomeRegularTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            manager.Add(split);

            // setup: create split task with only part1 and part2
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.PartsOf(split).Count() == 2);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0) == part1);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1) == part2);
            Assert.IsTrue(split.Duration == TimeSpan.FromDays(1));
            Assert.IsTrue(part1.Duration == TimeSpan.FromDays(0.5f));
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(0.5f));

            // test: join the 2 parts, leave only a non-split task left
            manager.Join(part1, part2);
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
            Assert.IsTrue(split.Duration == TimeSpan.FromDays(1));
        }

        [Test]
        public void MergeSplittedTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            var part4 = new Task();
            manager.Add(split);

            // setup: create a 4 part split task
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            manager.Split(part1, part3, TimeSpan.FromDays(1));
            manager.Split(part3, part4, TimeSpan.FromDays(1));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(manager.IsPart(part4));
            Assert.IsTrue(manager.PartsOf(split).Count() == 4);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(0) == part1);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(1) == part3);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(2) == part4);
            Assert.IsTrue(manager.PartsOf(split).ElementAt(3) == part2);
            Assert.IsTrue(manager.Tasks.Count() == 1);

            // test: merge the split task
            manager.Merge(split);
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
            Assert.IsTrue(!manager.IsPart(part3));
            Assert.IsTrue(!manager.IsPart(part4));
            Assert.IsTrue(manager.Tasks.Count() == 1);
            Assert.IsTrue(split.Duration == TimeSpan.FromDays(1));
        }

        [Test]
        public void GroupPartsBecomeGroupSplitTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task() { Name = "split" };
            var part1 = new Task() { Name = "part1" };
            var part2 = new Task() { Name = "part2" };
            var group = new Task() { Name = "group" };
            var task = new Task() { Name = "task" };
            manager.Add(split);
            manager.Add(group);
            manager.Add(task);

            // setup: create a split task and a group
            manager.Split(split, part1, part2, TimeSpan.FromDays(2));
            manager.Group(group, task);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(task));

            // test: group part into task;
            manager.Group(group, part1);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(task));
            Assert.IsTrue(!manager.IsMember(part1));
            Assert.IsTrue(manager.IsMember(split));
            Assert.IsTrue(manager.DirectMembersOf(group).Count() == 2);
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(task));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(split));

            // test: group task into part (no effect)
            manager.Group(part1, task);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(task));
            Assert.IsTrue(!manager.IsMember(part1));
            Assert.IsTrue(!manager.IsGroup(part1));
            Assert.IsTrue(manager.DirectMembersOf(group).Count() == 2);
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(task));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(split));
        }

        [Test]
        public void UngroupPartBecomeUngroupSplitTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task() { Name = "split" };
            var part1 = new Task() { Name = "part1" };
            var part2 = new Task() { Name = "part2" };
            var group = new Task() { Name = "group" };
            var task = new Task() { Name = "task" };
            manager.Add(split);
            manager.Add(group);
            manager.Add(task);

            // setup: group split task into group
            manager.Split(split, part1, part2, TimeSpan.FromDays(2));
            manager.Group(group, task);
            manager.Group(group, split);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(task));
            Assert.IsTrue(!manager.IsMember(part1));
            Assert.IsTrue(manager.IsMember(split));
            Assert.IsTrue(manager.DirectMembersOf(group).Count() == 2);
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(task));
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(split));

            // test: ungroup part from group
            manager.Ungroup(group, part1);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsGroup(group));
            Assert.IsTrue(manager.IsMember(task));
            Assert.IsTrue(!manager.IsMember(part1));
            Assert.IsTrue(!manager.IsMember(split));
            Assert.IsTrue(manager.DirectMembersOf(group).Count() == 1);
            Assert.IsTrue(manager.DirectMembersOf(group).Contains(task));
            Assert.IsTrue(!manager.DirectMembersOf(group).Contains(split));
        }

        [Test]
        public void JoinPartConfirmSchedule()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            manager.Add(split);

            // setup: create a 3 part split
            manager.SetStart(split, TimeSpan.FromDays(16));
            manager.SetDuration(split, TimeSpan.FromDays(24));
            manager.Split(split, part1, part2, TimeSpan.FromDays(8));
            manager.Split(part2, part3, TimeSpan.FromDays(8));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.PartsOf(split).Count() == 3);
            Assert.IsTrue(part1.Duration == TimeSpan.FromDays(8));
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(8));
            Assert.IsTrue(part3.Duration == TimeSpan.FromDays(8));
            Assert.IsTrue(split.Duration == TimeSpan.FromDays(24));
            Assert.IsTrue(split.Start == TimeSpan.FromDays(16));
            Assert.IsTrue(split.End == TimeSpan.FromDays(40));

            // setup: join parts 1 and 2, intentionally in bad order, but should still work
            manager.Join(part2, part1);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.PartsOf(split).Count() == 2);
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(16));
            Assert.IsTrue(part3.Duration == TimeSpan.FromDays(8));
            Assert.IsTrue(split.Duration == TimeSpan.FromDays(24)); // schedule of other parts not affected
            Assert.IsTrue(split.Start == TimeSpan.FromDays(16));
            Assert.IsTrue(split.End == TimeSpan.FromDays(40));
        }

        [Test]
        public void JoinIntoMerge()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            manager.Add(split);

            // setup: create a split task
            manager.Split(split, part1, part2, TimeSpan.FromDays(7));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));

            // test: join the two parts, resulting in split becoming back ito regular task
            manager.Join(part1, part2);
            Assert.IsTrue(!manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
        }

        [Test]
        public void DeleteMiddlePart()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            manager.Add(split);

            // setup: create a 3 part split task
            manager.SetDuration(split, TimeSpan.FromDays(30));
            manager.Split(split, part1, part2, TimeSpan.FromDays(4));
            manager.Split(part2, part3, TimeSpan.FromDays(5));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(part1.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(part2.Start == TimeSpan.FromDays(4));
            Assert.IsTrue(part3.Start == TimeSpan.FromDays(9));
            Assert.IsTrue(part1.Duration == TimeSpan.FromDays(4));
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(5));
            Assert.IsTrue(part3.Duration == TimeSpan.FromDays(21));
            Assert.IsTrue(split.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(split.End == TimeSpan.FromDays(30));

            // test: delete part2, part1 and part3 should not be affected
            manager.Delete(part2);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(!manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(part1.Duration == TimeSpan.FromDays(4));
            Assert.IsTrue(part3.Duration == TimeSpan.FromDays(21));
            Assert.IsTrue(part1.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(part3.Start == TimeSpan.FromDays(9));
            Assert.IsTrue(part3.End == TimeSpan.FromDays(30));
            Assert.IsTrue(split.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(split.End == TimeSpan.FromDays(30));
        }

        [Test]
        public void DeleteLastPart()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            manager.Add(split);

            // setup: create a 3 part split task
            manager.SetDuration(split, TimeSpan.FromDays(30));
            manager.Split(split, part1, part2, TimeSpan.FromDays(4));
            manager.Split(part2, part3, TimeSpan.FromDays(5));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(part1.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(part2.Start == TimeSpan.FromDays(4));
            Assert.IsTrue(part3.Start == TimeSpan.FromDays(9));
            Assert.IsTrue(part1.Duration == TimeSpan.FromDays(4));
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(5));
            Assert.IsTrue(part3.Duration == TimeSpan.FromDays(21));
            Assert.IsTrue(split.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(split.End == TimeSpan.FromDays(30));

            // test: delete part3, part1 and part2 should not be affected
            manager.Delete(part3);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(!manager.IsPart(part3));
            Assert.IsTrue(part1.Duration == TimeSpan.FromDays(4));
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(5));
            Assert.IsTrue(split.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(split.End == TimeSpan.FromDays(9));
        }

        [Test]
        public void DeleteFirstPart()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            manager.Add(split);

            // setup: create a 3 part split task
            manager.SetDuration(split, TimeSpan.FromDays(30));
            manager.Split(split, part1, part2, TimeSpan.FromDays(4));
            manager.Split(part2, part3, TimeSpan.FromDays(5));
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(part1.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(part2.Start == TimeSpan.FromDays(4));
            Assert.IsTrue(part3.Start == TimeSpan.FromDays(9));
            Assert.IsTrue(part1.Duration == TimeSpan.FromDays(4));
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(5));
            Assert.IsTrue(part3.Duration == TimeSpan.FromDays(21));
            Assert.IsTrue(split.Start == TimeSpan.FromDays(0));
            Assert.IsTrue(split.End == TimeSpan.FromDays(30));

            // test: delete part1, part2 and part3 should not be affected
            manager.Delete(part1);
            Assert.IsTrue(manager.IsSplit(split));
            Assert.IsTrue(!manager.IsPart(part1));
            Assert.IsTrue(manager.IsPart(part2));
            Assert.IsTrue(manager.IsPart(part3));
            Assert.IsTrue(part2.Start == TimeSpan.FromDays(4));
            Assert.IsTrue(part3.Start == TimeSpan.FromDays(9));
            Assert.IsTrue(part2.Duration == TimeSpan.FromDays(5));
            Assert.IsTrue(part3.Duration == TimeSpan.FromDays(21));
            Assert.IsTrue(split.Start == TimeSpan.FromDays(4));
            Assert.IsTrue(split.End == TimeSpan.FromDays(30));
        }

        [Test]
        public void CannotSetCompleteOnSplitTask()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            manager.Add(split);

            // setup: create a split
            manager.SetComplete(split, 0.2f);
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(split.Complete == 0);
            Assert.IsTrue(part1.Complete == 0);
            Assert.IsTrue(part2.Complete == 0);

            // test: changing complete on split will not have effect
            manager.SetComplete(split, 10);
            Assert.IsTrue(split.Complete == 0);
            Assert.IsTrue(part1.Complete == 0);
            Assert.IsTrue(part2.Complete == 0);
        }

        [Test]
        public void AdjustGroupDurationOnSplit()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group = new Task();
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            manager.Add(split);
            manager.Add(group);

            // setup: create a split under a group
            manager.Group(group, split);
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            Assert.IsTrue(group.Duration == split.Duration);
            Assert.IsTrue(group.End == part2.End);

            // test: split somemore and ensure group duration and end adjust correctly
            manager.Split(part2, part3, TimeSpan.FromDays(1));
            Assert.IsTrue(group.Duration == split.Duration);
            Assert.IsTrue(group.End == part3.End);
        }

        [Test]
        public void AdjustGroupDuringJoin()
        {
            IProjectManager<Task, object> manager = new ProjectManager<Task, object>("Testing");
            var group = new Task();
            var split = new Task();
            var part1 = new Task();
            var part2 = new Task();
            var part3 = new Task();
            manager.Add(split);
            manager.Add(group);

            // setup: create a split under a group
            manager.Group(group, split);
            manager.Split(split, part1, part2, TimeSpan.FromDays(1));
            manager.Split(part2, part3, TimeSpan.FromDays(1));
            Assert.IsTrue(group.Duration == TimeSpan.FromDays(1));
            Assert.IsTrue(group.Duration == split.Duration);
            Assert.IsTrue(group.End == part3.End);

            // test: join and make sure group duration is shortened
            manager.Join(part1, part3);
            Assert.IsTrue(!manager.IsPart(part3));
            Assert.IsTrue(group.Duration == TimeSpan.FromDays(1));
            Assert.IsTrue(group.Duration == split.Duration);
            Assert.IsTrue(group.End == part2.End);
        }
    }
}

