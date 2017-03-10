<%@ Page Language="C#" AutoEventWireup="true" Inherits="Glass.PublishViewer.PublishViewerPage, Glass.PublishViewer" %>

<%@ Import Namespace="Sitecore.Jobs" %>

<!DOCTYPE html>

<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta charset="utf-8" />
    <title>Publish Viewer - <%=Model.Jobs.Count() %> Jobs</title>
    <style>
        body {
            font-family: verdana;
            font-size: 8pt;
            margin: 0px;
        }

        img.logo {
            width: 100px;
        }

        table {
            border-collapse: collapse;
            border-spacing: 0;
        }

            table.fullWidth {
                min-width: 100%;
            }

            table.stats td {
                text-align: center;
                width: 200px;
                font-size: 10pt;
            }

        tr.thead th, thead {
            background-color: #5E5E5E;
            color: white;
        }

        tr.Finished {
            background-color: #eeeeee;
        }

        tr.Running {
            background-color: #00ff00;
        }

        th {
            padding: 2px;
        }

        td {
            padding: 2px;
            border: 1px solid #ccc;
        }

            td.nestedTable {
                padding: 0px;
            }
    </style>
</head>
<body>
    <div>
        <table class="stats">
            <tr class="thead">
                <td rowspan="2">
                    <a href="http://www.glass.lu" target="_blank">
                        <img src="logo-250.png" class="logo" />
                    </a>
                </td>
                <th>Monitoring Started</th>
                <th>Publishes</th>
                <th>Items Published</th>
                <th>Average</th>
            </tr>
            <tr>
                <td><%=Model.Stats.MonitorStart.ToLocalTime().ToString("HH:mm:ss dd-MM-yyyy") %></td>
                <td class="nestedTable">
                    <table>
                        <tr>
                            <td>Total:</td>
                            <td><%=Model.Stats.NumberOfPublishes %></td>
                        </tr>
                         <tr>
                            <td>Queued:</td>
                            <td><%=Model.Stats.NumberOfQueuedPublishes %></td>
                        </tr>
                        <tr>
                            <td>Cancelled:</td>
                            <td><%=Model.Stats.NumberOfCancelledPublishes %></td>
                        </tr>
                        <tr>
                            <td>Completed:</td>
                            <td><%=Model.Stats.NumberOfCompletedPublishes %></td>
                        </tr>
                    </table>
                </td>
                <td><%=Model.Stats.ItemsPublished %></td>
               
                <td class="nestedTable">
                    <table>
                        <tr>
                            <td>Queue Time:</td>
                            <td><%=Model.Stats.AverageQueueTime.ToString(@"hh\:mm\:ss")%></td>
                        </tr>
                        <tr>
                            <td>Time Per Item:</td>
                            <td><%= Model.Stats.AverageTimePerItem.ToString("F") %> sec
                        <br />
                                (Target: <%= Model.Targets.AverageTimePerItem %> sec)

                            </td>
                        </tr>
                    </table>
                </td>
                <td>
                    <a href="?action=terminate">Terminate Running Process</a>
                </td>
            </tr>
        </table>
        <br/>
    </div>

    <div>
        <table class="fullWidth">
            <thead>
                <tr>
                    <th>Status</th>
                    <th>Delete</th>
                    <th>Time</th>
                    <th>Average Per Item</th>
                    <th>Owner</th>
                    <th>Path</th>
                    <th>Processed</th>
                    <th>Mode</th>
                    <th>Source DB</th>
                    <th>Target DB</th>
                    <th>Current DB</th>
                    <th>Languages</th>
                    <th>Current Language</th>
                    <th>Message</th>
                </tr>
            </thead>
            <%  foreach (var job in Model.Jobs)
                {
            %>
            <tr class="<%=job.Status %>">
                <td>
                    <%=job.Status %>
                </td>
                <td>
                    <% if (job.Status != JobState.Running && job.Status != JobState.Finished)
                        { %>
                    <a href='?action=delete&amp;id=<%=job.Handle %>'>Delete</a>
                    <%   } %>
                </td>
                <td class="nestedTable">
                    <table>
                        <tr>
                            <td>Queue:</td>
                            <td><%=job.QueueTime.ToLocalTime().ToString("HH:mm:ss") %></td>
                        </tr>
                        <tr>
                            <td>Start:</td>
                            <td><%=job.StartTime.HasValue ? job.StartTime.Value.ToLocalTime().ToString("HH:mm:ss") :string.Empty %></td>
                        </tr>
                        <tr>
                            <td>Queue Duration:</td>
                            <td><%=job.QueueDuration.ToString(@"hh\:mm\:ss") %></td>
                        </tr>
                        <tr>
                            <td>End:</td>
                            <td><%=job.EndTime.HasValue ? job.EndTime.Value.ToString("HH:mm:ss") :string.Empty %></td>
                        </tr>
                        <tr>
                            <td>Processing Duration:</td>
                            <td><%=job.ProcessingDuration.ToString(@"hh\:mm\:ss") %></td>
                        </tr>
                    </table>

                </td>
                <td>
                    <%=job.AverageTimePerItem.ToString("F") %> sec
                </td>
                <td>
                    <%=job.Owner %>
                </td>
                <td>
                    <%=job.ItemName %>
                </td>
                <td>
                    <%=job.Processed %>
                </td>
             

                <td>
                    <%=job.Mode %>
                </td>
                <td>
                    <%=job.SourceDatabase %>
                </td>
                <td>
                    <%=job.TargetDatabase.Any() ? job.TargetDatabase.Aggregate((x,y)=>x+"<br />"+y) : string.Empty %> 
                </td>
                <td>
                    <%=job.CurrentTargetDatabase %>
                </td>
                <td>
                    <%=job.Languages.Any() ? job.Languages.Aggregate((x,y)=>x+"<br />"+y) : string.Empty %>
                </td>
                <td>
                    <%=job.CurrentLanguage %>
                </td>
                <td>
                    <%=job.Message.Any() ? job.Message.Aggregate((x,y)=>x+"<br />"+y) : string.Empty %>
                </td>
            </tr>
            <% } %>
        </table>

    </div>
    <script type="text/javascript">

        function refresh() {
            window.location.reload(true);
        }

        setTimeout(refresh, 1000);
    </script>
</body>
</html>
