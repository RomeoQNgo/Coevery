﻿using System;
using Coevery.Data.Migration;

namespace Coevery.Core.Scheduling {
    public class Migrations : DataMigrationImpl {

        public int Create() {
            
            SchemaBuilder.CreateTable("ScheduledTaskRecord", 
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<string>("TaskType")
                    .Column<DateTime>("ScheduledUtc")
                    .Column<int>("ContentItemVersionRecord_id")
                );

            return 1;
        }
    }
}