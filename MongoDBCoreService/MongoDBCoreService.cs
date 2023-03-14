//-----------------------------------------------------------------------
// <copyright file="MongoDBCoreService.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using MongoDBCoreService.db;
using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Json.Newtonsoft;
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.Settings;

namespace MongoDBCoreService
{
    // Represents the default type of the TwinCAT HMI server extension.
    public class MongoDBCoreService : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly ConfigListener _configListener = new ConfigListener();
        private readonly ShutdownListener _shutdownListener = new ShutdownListener();
        private MongoDBData _mongoData;
        private string _connectionString;
        private string _databaseName;

        // Called after the TwinCAT HMI server loaded the server extension.
        public ErrorValue Init()
        {
            // Wait for a debugger to be attached to the current process and signal a
            // breakpoint to the attached debugger in Init
            //TcHmiApplication.AsyncDebugHost.WaitForDebugger(true);

            //Event registers
            _requestListener.OnRequest += OnRequest;
            _configListener.OnChange += OnChange;
            _shutdownListener.OnShutdown += OnShutdown;

            //set up the config listener
            var settings = new ConfigListenerSettings();
            var filter = new ConfigListenerSettingsFilter(
                ConfigChangeType.OnChange, new string[] { "ConnectionString", "DatabaseName" }
            );
            settings.Filters.Add(filter);
            TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _configListener, settings);

            //Init MongoDB Class
            _connectionString = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "ConnectionString");
            _databaseName = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "DatabaseName");
            _mongoData = new MongoDBData(_databaseName, _connectionString);

            //Log
            TcHmiAsyncLogger.Send(Severity.Info, "MESSAGE_INIT", "");

            return ErrorValue.HMI_SUCCESS;
        }

        // Called when the extension gets disabled or the TwinCAT HMI server shutdown/reboots 
        private void OnShutdown(object sender, TcHmiSrv.Core.Listeners.ShutdownListenerEventArgs.OnShutdownEventArgs e)
        {
            //Log
            TcHmiAsyncLogger.Send(Severity.Info, "MESSAGE_SHUTDOWN", "");

            //Unregister listeners
            TcHmiApplication.AsyncHost.UnregisterListener(TcHmiApplication.Context, _requestListener);
            TcHmiApplication.AsyncHost.UnregisterListener(TcHmiApplication.Context, _shutdownListener);
            TcHmiApplication.AsyncHost.UnregisterListener(TcHmiApplication.Context, _configListener);
        }

        // Called when the user changes data in the config-page of the extension. Also called on extension init. 
        private void OnChange(object sender, TcHmiSrv.Core.Listeners.ConfigListenerEventArgs.OnChangeEventArgs e)
        {
            //Retrieve ConfigPage Values
            var connectionStringValue = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "ConnectionString");
            var databaseNameValue = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "DatabaseName");
            //Update value if new
            if (connectionStringValue != _connectionString || databaseNameValue != _databaseName)
            {
                _connectionString = connectionStringValue;
                _databaseName = databaseNameValue;
                //Update DB connection
                _mongoData = new MongoDBData(_databaseName, _connectionString);

                //Log
                TcHmiAsyncLogger.Send(e.Context, Severity.Info, "NEW_CONFIG", "");
            }
        }

        // Called when a client requests a symbol from the domain of the TwinCAT HMI server extension.
        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            try
            {
                e.Commands.Result = MongoDBCoreServiceErrorValue.Success;

                foreach (Command command in e.Commands)
                {
                    try
                    {
                        // Use the mapping to check which command is requested
                        switch (command.Mapping)
                        {
                            case "CollectionList":
                                CollectionList(command, e.Context);
                                break;

                            case "AggregationOutputList1":
                                AggregationOutputList(command, e.Context, 1);
                                break;

                            case "AggregationOutputList2":
                                AggregationOutputList(command, e.Context, 2);
                                break;

                            case "AggregationOutputList3":
                                AggregationOutputList(command, e.Context, 3);
                                break;

                            case "UpdateDocument":
                                UpdateDocument(command, e.Context);
                                break;

                            case "CreateDocument":
                                CreateDocument(command, e.Context);
                                break;

                            case "RemoveDocument":
                                RemoveDocument(command, e.Context);
                                break;

                            case "CreateCollection":
                                CreateCollection(command, e.Context);
                                break;

                            case "SetAggregationPipeline":
                                SetAggregationPipeline(command, e.Context);
                                break;

                            case "GetAggregationResult":
                                GetAggregationResult(command, e.Context);
                                break;

                            default:
                                command.ExtensionResult = MongoDBCoreServiceErrorValue.Fail;
                                command.ResultString = "Unknown command '" + command.Mapping + "' not handled.";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        command.ExtensionResult = MongoDBCoreServiceErrorValue.Fail;
                        command.ResultString = "Calling command '" + command.Mapping + "' failed! Additional information: " + ex.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                TcHmiAsyncLogger.Send(e.Context, Severity.Error, "ERROR_CALL_COMMAND", new string[] { ex.Message });
            }
        }

        //Returns all collections as object arrays inside the specified database
        private ErrorValue CollectionList(Command command, Context context)
        {
            if (_mongoData.CollectionList != null)
            {
                try
                {
                    command.ReadValue = TcHmiJsonSerializer.Serialize(_mongoData.CollectionList);
                }
                catch (Exception ex)
                {
                    TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_REQUEST", new string[] { ex.Message });
                }
            }

            return ErrorValue.HMI_SUCCESS;
        }

        //Returns the aggregation output as a list
        private ErrorValue AggregationOutputList(Command command, Context context, int pipelineId)
        {
            try
            {
                switch (pipelineId)
                {
                    case 1:
                        command.ReadValue = TcHmiJsonSerializer.Serialize(_mongoData.AggregationOutputList1);
                        break;

                    case 2:
                        command.ReadValue = TcHmiJsonSerializer.Serialize(_mongoData.AggregationOutputList2);
                        break;

                    case 3:
                        command.ReadValue = TcHmiJsonSerializer.Serialize(_mongoData.AggregationOutputList3);
                        break;
                }
            }
            catch (Exception ex)
            {
                TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_REQUEST", new string[] { ex.Message });
            }

            return ErrorValue.HMI_SUCCESS;
        }

        //Update specific document by _id
        private ErrorValue UpdateDocument(Command command, Context context)
        {
            if (command.WriteValue != null && command.WriteValue.Type == TcHmiSrv.Core.ValueType.Struct)
            {
                try
                {
                    var writeValue = command.WriteValue;
                    var collectionName = writeValue["collection"];
                    var jsonDoc = TcHmiJsonSerializer.Serialize(writeValue["data"]);
                    _mongoData.UpdateDocument(collectionName, jsonDoc);
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.Success;
                }
                catch (Exception ex)
                {
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.UpdateDocumentFail;
                    TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_REQUEST", new string[] { ex.Message });
                }
            }
            else
            {
                command.ExtensionResult = MongoDBCoreServiceErrorValue.DataWrongTypeOrEmpty;
            }

            command.ReadValue = command.WriteValue;
            return ErrorValue.HMI_SUCCESS;
        }

        //Create a new document inside a collection. Also creates collection if it not exists
        private ErrorValue CreateDocument(Command command, Context context)
        {
            if (command.WriteValue != null && command.WriteValue.Type == TcHmiSrv.Core.ValueType.Struct)
            {
                try
                {
                    var writeValue = command.WriteValue;
                    var collectionName = writeValue["collection"];
                    var jsonDoc = TcHmiJsonSerializer.Serialize(writeValue["data"]);
                    _mongoData.CreateDocument(collectionName, jsonDoc);
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.Success;
                }
                catch (Exception ex)
                {
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.CreateDocumentFail;
                    TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_REQUEST", new string[] { ex.Message });
                }
            }
            else
            {
                command.ExtensionResult = MongoDBCoreServiceErrorValue.DataWrongTypeOrEmpty;
            }

            command.ReadValue = command.WriteValue;
            return ErrorValue.HMI_SUCCESS;
        }

        //Removes a document inside a collection
        private ErrorValue RemoveDocument(Command command, Context context)
        {
            if (command.WriteValue != null && command.WriteValue.Type == TcHmiSrv.Core.ValueType.Struct)
            {
                try
                {
                    var writeValue = command.WriteValue;
                    var collectionName = writeValue["collection"];
                    var id = writeValue["id"];
                    _mongoData.RemoveDocument(collectionName, id);
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.Success;
                }
                catch (Exception ex)
                {
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.RemoveDocumentFail;
                    TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_REQUEST", new string[] { ex.Message });
                }
            }
            else
            {
                command.ExtensionResult = MongoDBCoreServiceErrorValue.DataWrongTypeOrEmpty;
            }

            command.ReadValue = command.WriteValue;
            return ErrorValue.HMI_SUCCESS;
        }

        //Create a new collection in the database
        private ErrorValue CreateCollection(Command command, Context context)
        {
            if (command.WriteValue != null && command.WriteValue.Type == TcHmiSrv.Core.ValueType.String)
            {
                try
                {
                    _mongoData.CreateCollection(command.WriteValue);
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.Success;
                }
                catch (Exception ex)
                {
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.CreateCollectionFail;
                    TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_REQUEST", new string[] { ex.Message });
                }
            }
            else
            {
                command.ExtensionResult = MongoDBCoreServiceErrorValue.DataWrongTypeOrEmpty;
            }

            command.ReadValue = command.WriteValue;
            return ErrorValue.HMI_SUCCESS;
        }

        //Set the aggregation pipeline query
        private ErrorValue SetAggregationPipeline(Command command, Context context)
        {
            if (command.WriteValue != null && command.WriteValue.Type == TcHmiSrv.Core.ValueType.Struct)
            {
                try
                {
                    var writeValue = command.WriteValue;
                    var collectionName = writeValue["collection"];
                    var pipelineString = writeValue["data"].ToString();
                    int pipelineId = writeValue["pipelineId"].ToInt32();

                    switch (pipelineId)
                    {
                        case 1:
                            _mongoData.SetAggregationPipeline1(collectionName, pipelineString);
                            break;

                        case 2:
                            _mongoData.SetAggregationPipeline2(collectionName, pipelineString);
                            break;

                        case 3:
                            _mongoData.SetAggregationPipeline3(collectionName, pipelineString);
                            break;
                    }

                    command.ExtensionResult = MongoDBCoreServiceErrorValue.Success;
                }
                catch (Exception ex)
                {
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.SetAggregationPipelineFail;
                    TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_REQUEST", new string[] { ex.Message });
                }
            }
            else
            {
                command.ExtensionResult = MongoDBCoreServiceErrorValue.DataWrongTypeOrEmpty;
            }

            command.ReadValue = command.WriteValue;
            return ErrorValue.HMI_SUCCESS;
        }

        //Gets the aggregation pipeline query results and returns it to the caller
        private ErrorValue GetAggregationResult(Command command, Context context)
        {
            if (command.WriteValue != null && command.WriteValue.Type == TcHmiSrv.Core.ValueType.Struct)
            {
                try
                {
                    var writeValue = command.WriteValue;
                    var collectionName = writeValue["collection"];
                    var pipelineString = writeValue["data"].ToString();

                    var result = _mongoData.GetAggregationResult(collectionName, pipelineString);

                    command.ExtensionResult = MongoDBCoreServiceErrorValue.Success;
                    command.ReadValue = TcHmiJsonSerializer.Serialize(result);
                }
                catch (Exception ex)
                {
                    command.ExtensionResult = MongoDBCoreServiceErrorValue.GetAggregationResultFail;
                    TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_REQUEST", new string[] { ex.Message });
                }
            }
            else
            {
                command.ExtensionResult = MongoDBCoreServiceErrorValue.DataWrongTypeOrEmpty;
                command.ReadValue = command.WriteValue;
            }
          
            return ErrorValue.HMI_SUCCESS;
        }
    }
}