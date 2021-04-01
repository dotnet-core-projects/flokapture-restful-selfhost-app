using BusinessLayer.DbEntities;
using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessLayer.ExtensionLibrary
{
    public static class TypeExtensions
    {
        public static List<TreeView> ToTreeView<T>(this List<T> listItems) where T : StatementReferenceMaster, new()
        {
            if (!listItems.Any()) return new List<TreeView>();
            return listItems.Select(listItem => new TreeView
            {
                FileId = listItem.FileId,
                GraphId = Guid.NewGuid().ToString("N"),
                BaseCommandId = listItem.BaseCommandId,
                MethodCalled = listItem.MethodName,
                ActualStatementId = listItem._id,
                AlternateName = listItem.AlternateName,
                ClassCalled = listItem.ClassNameDeclared,
                GraphName = listItem.OriginalStatement,
                GroupId = 0,
                GroupName = "",
                IndentLevel = 0,
                NodeId = new Random().Next(),
                ParentId = "-1",
                PrimaryCommandId = 0
            }).ToList();
        } 
        public static List<TreeView> IfBlockStatement(this List<TreeView> treeView, List<TreeView> lstTreeView)
        {
            int indexPosition = -1;
            int ifCounter = 0;
            foreach (var treeItem in treeView)
            {
                indexPosition++;
                if (treeItem.BaseCommandId != 1) continue; 
                var treeViewList = new List<TreeView>();
                for (int i = indexPosition; i < lstTreeView.Count; i++)
                {
                    treeViewList.Add(lstTreeView[i]);
                    if (lstTreeView[i].BaseCommandId == 1 )
                        ifCounter++;
                    if (lstTreeView[i].BaseCommandId == 2)
                        ifCounter--;
                    if (ifCounter == 0)
                        break;
                }
                var prevParentId = treeViewList.First().ParentId;
                var graphId = Guid.NewGuid().ToString("N"); 
                treeViewList.First().GraphId = graphId;
                for (int j = 1; j < treeViewList.Count; j++)
                {
                    if (treeViewList[j].ParentId != prevParentId) continue;
                    treeViewList[j].ParentId = graphId;

                    treeViewList[j].IndentLevel = treeViewList[j].IndentLevel + 2;
                    if (treeViewList[j].BaseCommandId == 2)
                    {
                        treeViewList[j].IndentLevel = treeViewList.First().IndentLevel;
                    }
                }
            }
            return lstTreeView;
        }
        public static List<TreeView> MethodBlockStatement(this List<TreeView> treeView, List<TreeView> lstTreeView)
        {
            int indexPosition = -1;
            int ifCounter = 0;
            foreach (var treeItem in treeView)
            {
                indexPosition++;
                if (treeItem.BaseCommandId != 8) continue;

                var treeViewList = new List<TreeView>();
                for (int i = indexPosition; i < lstTreeView.Count; i++)
                {
                    treeViewList.Add(lstTreeView[i]);
                    if (lstTreeView[i].BaseCommandId == 8)
                        ifCounter++;
                    if (lstTreeView[i].BaseCommandId == 9)
                        ifCounter--;
                    if (ifCounter == 0)
                        break;
                }
                var prevParentId = treeViewList.First().ParentId;
                var graphId = Guid.NewGuid().ToString("N");
                treeViewList.First().GraphId = graphId;
                for (int j = 1; j < treeViewList.Count; j++)
                {
                    if (treeViewList[j].ParentId != prevParentId) continue;
                    treeViewList[j].ParentId = graphId;

                    treeViewList[j].IndentLevel = treeViewList[j].IndentLevel + 2;
                    if (treeViewList[j].BaseCommandId == 9)
                    {
                        treeViewList[j].IndentLevel = treeViewList.First().IndentLevel;
                    }
                }
            }
            return lstTreeView;
        }

        public static List<TreeView> LoopBlockStatement(this List<TreeView> treeView, List<TreeView> lstTreeView)
        {
            int indexPosition = -1;
            int loopCounter = 0;

            foreach (var treeItem in treeView)
            {
                indexPosition++;
                if (treeItem.BaseCommandId != 3) continue;

                var treeViewList = new List<TreeView>();
                for (int i = indexPosition; i < lstTreeView.Count; i++)
                {
                    treeViewList.Add(lstTreeView[i]);
                    if (lstTreeView[i].BaseCommandId == 3)
                        loopCounter++;
                    if (lstTreeView[i].BaseCommandId == 4)
                        loopCounter--;
                    if (loopCounter == 0)
                        break;
                }
                int curIndentLevel = treeViewList.First().IndentLevel;
                var prevParentId = treeViewList.First().ParentId;
                var graphId = Guid.NewGuid().ToString("N");
                treeViewList.First().GraphId = graphId;
                treeViewList.First().IndentLevel = curIndentLevel + 1;
                for (int j = 1; j < treeViewList.Count; j++)
                {
                    if (treeViewList[j].ParentId != prevParentId) continue;
                    treeViewList[j].ParentId = graphId;
                    var childItems = (from s in treeView where s.ParentId == treeViewList[j].GraphId select s).ToList();
                    childItems.ForEach(c => { c.IndentLevel = c.IndentLevel + 2; });
                    treeViewList[j].IndentLevel = treeViewList[j].IndentLevel + 2;
                    if (treeViewList[j].BaseCommandId == 4)
                    {
                        treeViewList[j].IndentLevel = curIndentLevel + 1;
                    }
                }
            }
            return treeView;
        }

        public static List<TreeView> ElseBlockStatement(this List<TreeView> treeView, List<TreeView> lstTreeView)
        {
            int indexPosition = -1;
            foreach (var treeItem in treeView)
            {
                indexPosition++;
                if (treeItem.BaseCommandId != 10) continue;
                int endIfCounter = -1;

                var treeViewList = new List<TreeView>();
                for (var i = indexPosition; i < lstTreeView.Count; i++)
                {
                    treeViewList.Add(lstTreeView[i]);
                    if (lstTreeView[i].BaseCommandId == 1)
                        endIfCounter--;
                    if (lstTreeView[i].BaseCommandId == 2)
                        endIfCounter++;
                    if (endIfCounter == 0)
                        break;
                }
                int curIndentLevel = treeViewList.First().IndentLevel;
                var prevParentId = treeViewList.First().ParentId;
                var graphId = Guid.NewGuid().ToString(); // "ElseBlock" + indexPosition + treeItem.ActualStatementId;
                treeViewList.First().GraphId = graphId;
                var parentIf = treeView.Find(f => f.GraphId == treeViewList.First().ParentId);
                treeViewList.First().IndentLevel = parentIf.IndentLevel;
                for (var j = 1; j < treeViewList.Count; j++)
                {
                    if (treeViewList[j].BaseCommandId == 2 || treeViewList[j].BaseCommandId == 9) continue;
                    if (treeViewList[j].ParentId != prevParentId) continue;

                    treeViewList[j].ParentId = graphId;
                    treeViewList[j].IndentLevel = curIndentLevel + 1;
                }
            }
            return treeView;
        }

    }
}
