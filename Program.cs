using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace AzureDevopsCreateTasks
{
    static class Program
    {
        public static readonly Uri OrgUrl = new Uri("https://dev.azure.com/wkuk-git-vsts");
        public static readonly int workItemId = 0;   // ID of a work item, for example: 12

        /// <summary>
        /// steps to get personal access token
        //  See https://docs.microsoft.com/azure/devops/integrate/get-started/authentication/pats
        /// </summary>
        public static readonly string personalAccessToken = "";

        public static readonly List<TaskDetail> Tasks = new List<TaskDetail> {
                new TaskDetail("Analysis: On user story requirements & dependencies",2.00, 2.00),
                new TaskDetail("HMRC LLD: Analysis, development approach, LLD document update",3.00, 3.00),
                new TaskDetail("ERD: Database table design & discussion",2.00, 2.00),
                new TaskDetail("UI: Call API endpoint & create model",3.00, 3.00),
                new TaskDetail("UI: Component development",4.00, 4.00),
                new TaskDetail("UI: Unit test",2.00, 2.00),
                new TaskDetail("Compliance APIM: Add details of new operations in APIM",1.00, 1.00),
                new TaskDetail("Compliance API: Endpoint operation development",4.00, 4.00),
                new TaskDetail("Compliance xUnit: Unit test code coverage for new development",4.00, 4.00),
                new TaskDetail("HMRC APIM: Add details of new operations in APIM",1.00, 1.00),
                new TaskDetail("HMRC API: Endpoint operation development",4.00, 4.00),
                new TaskDetail("HMRC xUnit: Unit test code coverage for new development",4.00, 4.00),
                new TaskDetail("Functional test: Test functionality after deployment",2.00, 2.00),
                new TaskDetail("Raise PR, fix review comment",2.00, 2.00)
            };

        static void Main(string[] args)
        {

            // Create a connection
            VssConnection connection = new VssConnection(OrgUrl, new VssBasicCredential(string.Empty, personalAccessToken));

            // Show details a work item
            var parentWorkItem = GetWorkItemDetails(connection, workItemId);

            if (parentWorkItem?.Id != null)
            {
                Tasks.ForEach(task =>
                {
                // Create WorkItem
                CreateTask(connection, parentWorkItem, task);
                });
            }

        }

        /// <summary>
        /// Create a bug using the .NET client library
        /// </summary>
        /// <returns>Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>    
        public static void CreateTask(VssConnection connection, WorkItem parentWorkItem, TaskDetail taskDetail)
        {
            var parentLink = ((ReferenceLink)parentWorkItem.Links.Links.GetValueOrDefault("self")).Href;

            Console.WriteLine("Parent item id : " + parentWorkItem.Id);

            string parentIterationPath = parentWorkItem.Fields.GetValueOrDefault("System.IterationPath").ToString();
            string parentAreaPath = parentWorkItem.Fields.GetValueOrDefault("System.AreaPath").ToString();
            string parentTeamProject = parentWorkItem.Fields.GetValueOrDefault("System.TeamProject").ToString();

            JsonPatchDocument patchDocument = new JsonPatchDocument
            {

                //add fields and their values to your patch document
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = taskDetail.Title
                },

                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate",
                    Value = taskDetail.Estimated
                },

                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork",
                    Value = taskDetail.Remaining
                },

                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = parentLink
                    }
                },

                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.IterationPath",
                    Value = parentIterationPath
                },

                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.AreaPath",
                    Value = parentAreaPath
                },

                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.TeamProject",
                    Value = parentTeamProject
                }
            };

            WorkItemTrackingHttpClient workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();

            try
            {
                WorkItem result = workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, parentTeamProject, "Task").Result;

                Console.WriteLine("Task Successfully Created: Task #{0}", result.Id);
                Console.WriteLine("Task Successfully Created: Task Link {0}", ((ReferenceLink)result.Links.Links.GetValueOrDefault("html")).Href);

            }
            catch (AggregateException ex)
            {
                Console.WriteLine("Error creating Task: {0}", ex.InnerException.Message);
            }
        }

        public static WorkItem GetWorkItemDetails(VssConnection connection, int workItemId)
        {
            // Get an instance of the work item tracking client
            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            try
            {
                // Get the specified work item
                WorkItem workitem = witClient.GetWorkItemAsync(workItemId).Result;

                // Output the work item's field values
                //foreach (var field in workitem.Fields)
                //{
                //    Console.WriteLine("  {0}: {1}", field.Key, field.Value);
                //}

                return workitem;
            }
            catch (AggregateException aex)
            {
                VssServiceException vssex = aex.InnerException as VssServiceException;
                if (vssex != null)
                {
                    Console.WriteLine(vssex.Message);
                }
                return null;
            }
        }
    }

    class TaskDetail
    {
        public TaskDetail(string title, double estimated, double remaining)
        {
            this.Title = title;
            this.Estimated = estimated;
            this.Remaining = remaining;
        }

        public string Title { get; set; }
        public double Estimated { get; set; }
        public double Remaining { get; set; }
    }
}
