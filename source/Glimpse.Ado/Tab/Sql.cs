﻿using System;
using System.Collections.Generic;
using System.Linq;
using Glimpse.Ado.Messages;
using Glimpse.Ado.Plumbing.Models;
using Glimpse.Ado.Tab.Support;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.Core.Tab.Assist;

namespace Glimpse.Ado.Tab
{
    public class SQL : TabBase, ITabSetup, IKey, ITabLayout
    {
        public override string Name
        {
            get { return "SQL"; }
        }

        public string Key
        {
            get { return "glimpse_sql"; }
        }

        public void Setup(ITabSetupContext context)
        {
            context.PersistMessages<AdoMessage>();
        }

        public object GetLayout()
        {
            return Layout;
        }

        public override object GetData(ITabContext context)
        {
            var sanitizer = new CommandSanitizer();            
            var messages = context.GetMessages<AdoMessage>().ToList();
            var aggregator = new GlimpseDbQueryMessageAggregator(messages);
            var queryMetadata = aggregator.Aggregate();
               
            if (queryMetadata == null)
                return null;

            var connections = new List<object[]> { new[] { "Commands per Connection", "Open Time" } }; 
            foreach (var connection in queryMetadata.Connections.Values)
            {
                if (connection.Commands.Count == 0 && connection.Transactions.Count == 0)
                    continue;
                
                var commands = new List<object[]> { new[] { "Transaction Start", "Ordinal", "Command", "Parameters", "Records", "Command Time", "From First", "Transaction End", "Errors" } };
                var commandCount = 1;
                foreach (var command in connection.Commands.Values)
                {
                    //Transaction Start
                    List<object[]> headTransaction = null;
                    if (command.HeadTransaction != null)
                        headTransaction = new List<object[]> { new[] { "Name", "Isolation Level" }, new[] { "Transaction Started", command.HeadTransaction.IsolationLevel } };

                    //Transaction Finish
                    List<object[]> tailTransaction = null;
                    if (command.TailTransaction != null)
                        tailTransaction = new List<object[]> { new[] { "Name", "Committed" }, new[] { "Transaction Complete", command.TailTransaction.Committed ? "Committed" : "Rollbacked" } }; 
                     
                    //Parameters
                    List<object[]> parameters = null;
                    if (command.Parameters.Count > 0)
                    {
                        parameters = new List<object[]> { new[] { "Name", "Value", "Type", "Size" } };
                        foreach (var parameter in command.Parameters)
                            parameters.Add(new[] { parameter.Name, parameter.Value, parameter.Type, parameter.Size });
                    }

                    //Exception
                    List<object[]> errors = null;
                    if (command.Exception != null)
                    {
                        var exception = command.Exception.GetBaseException();
                        var exceptionName = command.Exception != exception ? command.Exception.Message + ": " + exception.Message : exception.Message;

                        errors = new List<object[]> { new[] { "Error", "Stack" }, new[] { exceptionName, exception.StackTrace } };
                    }

                    //Commands
                    var records = command.RecordsAffected == null || command.RecordsAffected < 0 ? command.TotalRecords : command.RecordsAffected;
                    commands.Add(new object[] { headTransaction, commandCount++, sanitizer.Process(command.Command, command.Parameters), parameters, records, command.ElapsedMilliseconds, "0", tailTransaction, errors, errors != null ? "error" : "" });
                }
                var elapse = 0.0;
                //TODO: Can we use a stopwatch here?
                if (connection.EndDateTime.HasValue && connection.StartDateTime.HasValue)
                    elapse = Convert.ToInt32(connection.EndDateTime.Value.Subtract(connection.StartDateTime.Value).TotalMilliseconds);
                connections.Add(new object[] { commands, elapse });
            }

            return connections.Count > 1 ? connections : null;
        }

        private static readonly object Layout = TabLayout.Create()
               .Row(r =>
               {
                   r.Cell(0).DisablePreview().SetLayout(TabLayout.Create().Row(x =>
                           x.Cell(0).SpanColumns(6).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                           {
                               y.Cell(0).WidthInPixels(150).AsKey();
                               y.Cell(1);
                           }))).Row(x =>
                           {
                               x.Cell(1).WidthInPixels(55);
                               x.Cell(2).AsCode(CodeType.Sql).DisablePreview();
                               x.Cell(3).WidthInPercent(25).DisablePreview();
                               x.Cell(4).WidthInPixels(60);
                               x.Cell(5).WidthInPixels(100).Suffix(" ms").Class("mono");
                               x.Cell(6).WidthInPixels(70).Prefix("T+ ").Suffix(" ms").Class("mono");
                           }).Row(x =>
                           x.Cell(8).SpanColumns(6).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                           {
                               y.Cell(0).WidthInPercent(20);
                               y.Cell(1).Class("mono").DisablePreview();
                           }))).Row(x =>
                           x.Cell(7).SpanColumns(6).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                           {
                               y.Cell(0).WidthInPixels(150).AsKey();
                               y.Cell(1);
                           }))));
                   r.Cell(1).WidthInPixels(75).Suffix(" ms").Class("mono");
               }).Build();        
    }
}

