﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace N.EntityFramework.Extensions.Util
{
    internal static class CommonUtil
    {
        internal static string GetStagingTableName(TableMapping tableMapping, bool usePermanentTable, SqlConnection sqlConnection)
        {
            string tableName = string.Empty;
            if (usePermanentTable)
                tableName = string.Format("[{0}].[tmp_be_xx_{1}_{2}]", tableMapping.Schema, tableMapping.TableName, sqlConnection.ClientConnectionId.ToString());
            else
                tableName = string.Format("[{0}].[#tmp_be_xx_{1}]", tableMapping.Schema, tableMapping.TableName);
            return tableName;
        }
        internal static IEnumerable<string> FormatColumns(IEnumerable<string> columns)
        {
            return columns.Select(s => s.StartsWith("[") && s.EndsWith("]") ? s : string.Format("[{0}]", s));
        }
        internal static IEnumerable<string> FormatColumns(string tableAlias, IEnumerable<string> columns)
        {
            return columns.Select(s => s.StartsWith("[") && s.EndsWith("]") ? string.Format("[{0}].{1}", tableAlias, s) : string.Format("[{0}].[{1}]", tableAlias, s));
        }
        internal static IEnumerable<string> FilterColumns<T>(IEnumerable<string> columnNames, string[] primaryKeyColumnNames, Expression<Func<T, object>> inputColumns = null, Expression<Func<T, object>> ignoreColumns = null)
        {
            var filteredColumnNames = columnNames;
            if (inputColumns != null)
            {
                var inputColumnNames = inputColumns.GetObjectProperties();
                filteredColumnNames = filteredColumnNames.Intersect(inputColumnNames);
            }
            if (ignoreColumns != null)
            {
                var ignoreColumnNames = ignoreColumns.GetObjectProperties();
                if (ignoreColumnNames.Intersect(primaryKeyColumnNames).Any())
                {
                    throw new InvalidDataException("Primary key columns can not be ignored in BulkInsertOptions.IgnoreColumns");
                }
                else
                {
                    filteredColumnNames = filteredColumnNames.Except(ignoreColumnNames);
                }
            }
            return filteredColumnNames;
        }
        internal static string FormatTableName(string tableName)
        {
            return string.Join(".", tableName.Split('.').Select(s => $"[{RemoveQualifier(s)}]"));
        }
        private static string RemoveQualifier(string name)
        {
            return name.TrimStart('[').TrimEnd(']');
        }
    }
    internal static class CommonUtil<T>
    {
        internal static string[] GetColumns(Expression<Func<T, T, bool>> expression, string[] tableNames)
        {
            List<string> foundColumns = new List<string>();
            string sqlText = (string)expression.Body.GetPrivateFieldValue("DebugView");

            int startIndex = sqlText.IndexOf("$");
            while (startIndex != -1)
            {
                int endIndex = sqlText.IndexOf(" ", startIndex);
                string column = endIndex == -1 ? sqlText.Substring(startIndex) : sqlText.Substring(startIndex, endIndex - startIndex);
                string[] columnParts = column.Split('.');
                if (tableNames == null || tableNames.Contains(columnParts[0].Remove(0, 1)))
                {
                    foundColumns.Add(columnParts[1]);
                }
                startIndex = sqlText.IndexOf("$", startIndex + 1);
            }

            return foundColumns.ToArray();
        }
        internal static string GetJoinConditionSql(Expression<Func<T, T, bool>> joinKeyExpression, string[] storeGeneratedColumnNames, string sourceTableName = "s", string targetTableName = "t")
        {
            string joinConditionSql = string.Empty;
            if (joinKeyExpression != null)
            {
                joinConditionSql = joinKeyExpression.ToSqlPredicate(sourceTableName, targetTableName);
            }
            else
            {
                int i = 1;
                foreach (var storeGeneratedColumnName in storeGeneratedColumnNames)
                {
                    joinConditionSql += (i > 1 ? " AND " : "") + string.Format("{0}.{2}={1}.{2}", sourceTableName, targetTableName, storeGeneratedColumnName);
                    i++;
                }
            }
            return joinConditionSql;
        }
    }
}