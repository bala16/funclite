using FuncLite.Client.BackendHelper;
using FuncLite.Client.BackendHelper.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FuncLite.Client
{
    class Program
    {
        const string backendURL = "https://functions.azure.com";

        static void Main(string[] args)
        {
            CommandLine cli = new CommandLine(backendURL);

            cli.RegisterRun();

            cli.RegisterList();
            cli.RegisterPublish();
            cli.RegisterDelete();
            cli.RegisterDetails();

            cli.RegisterGetVersions();

            cli.RegisterGetLogs();

            cli.Parse(args);
        }

        public class CommandLine
        {
            private FunctionsLite client;
            private CommandLineApplication application;
            public CommandLine(string url)
            {
                client = new FunctionsLite(new Uri(url));
                application = new CommandLineApplication();
                application.HelpOption("-? | -h | --help");
            }

            public int Parse(string[] args)
            {
                return application.Execute(args);
            }

            private void LogExecution(CommandLineApplication command)
            {
                Console.WriteLine("Executing " + command.Name + "...");
            }

            public void RegisterList()
            {
                application.Command("list", (command) => {
                    command.OnExecute(async () =>
                    {
                        LogExecution(command);
                        IList<Function> functions = await client.ListFunctionsAsync();
                        foreach(Function f in functions)
                        {
                            Console.WriteLine(f.ToString());
                        }
                        return 0;
                    });
                });
            }


            public void RegisterPublish()
            {
                CommandArgument name = null;
                CommandArgument path = null;
                application.Command("publish", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    name = command.Argument("path", "The files to be published");
                    command.OnExecute(async () =>
                    {
                        LogExecution(command);
                        //TODO create ZIP and submit
                        await client.CreateUpdateFunctionAsync(name.Value);
                        return 0;
                    });
                });
            }

            public void RegisterGetVersions()
            {
                CommandArgument name = null;
                application.Command("versions", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {
                        LogExecution(command);
                        IList<VersionInfo> versions = await client.ListVersionsForFunctionAsync(name.Value);
                        foreach (VersionInfo v in versions)
                        {
                            Console.WriteLine(v.ToString());
                        }
                        return 0;
                    });
                });
            }

            public void RegisterDetails()
            {
                CommandArgument name = null;
                CommandOption version = null;
                application.Command("detail", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    command.Option("-v | --version", "A specific version to read", CommandOptionType.SingleValue);
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {
                        LogExecution(command);
                        if (version.HasValue()) {
                            VersionInfo v = await client.GetVersionOfFunctionAsync(name.Value, version.Value());
                            Console.WriteLine(v.ToString());
                        }
                        else
                        {
                            //TODO
                            Console.WriteLine("NOT YET SUPPORTED");
                        }
                        
                        return 0;
                    });
                });
            }


            public void RegisterDelete()
            {
                CommandArgument name = null;
                CommandOption version = null;
                application.Command("delete", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    version = command.Option("-v | --version", "A specific version to delete", CommandOptionType.SingleValue);
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {
                        LogExecution(command);
                        if (version.HasValue())
                        {
                            //TODO confirmation
                            await client.DeleteVersionOfFunctionAsync(name.Value, version.Value());
                        }
                        else
                        {
                            //TODO confirmation
                            await client.DeleteFunctionAsync(name.Value);
                        }

                        return 0;
                    });
                });
            }


            public void RegisterGetLogs()
            {
                CommandArgument name = null;
                CommandOption invocation = null;
                application.Command("logs", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    invocation = command.Option("-i | --invocation", "A specific invocation to get logs for", CommandOptionType.SingleValue);
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {
                        LogExecution(command);

                        if (invocation.HasValue())
                        {
                            string logs = await client.GetLogForInvocationAsync(name.Value, invocation.Value());
                            Console.WriteLine(logs);
                        }
                        else
                        {
                            string logs = await client.GetLogStreamAsync(name.Value);
                            Console.WriteLine(logs);
                        }
                        return 0;
                    });
                });
            }


            //TODO
            public void RegisterRun()
            {
                CommandArgument name = null;
                CommandOption version = null;
                application.Command("run", (command) => {
                    name = command.Argument("functionName", "The name of the target function");
                    version = command.Option("-v | --version", "A specific version to run", CommandOptionType.SingleValue);
                    command.HelpOption("--help");
                    command.OnExecute(async () =>
                    {
                        LogExecution(command);
                        //if (version.HasValue())
                        //{

                        //}
                        //else
                        //{

                        //}
                        return 0;
                    });
                });
            }

        }

    }
}