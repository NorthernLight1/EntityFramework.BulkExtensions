﻿namespace N.EntityFramework.Extensions
{
    public class BulkMergeOutputRow<T>
    {
        public string Action { get; set; }
        //public T Item { get; set; }

        public BulkMergeOutputRow(string action)
        {
            this.Action = action;
            //this.Item = item;
        }
    }
}