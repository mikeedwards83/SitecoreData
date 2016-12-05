<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="Sitecore.Configuration" %>
<%@ Import Namespace="Sitecore.Data" %>
<%@ Import Namespace="Sitecore.Data.Fields" %>
<%@ Import Namespace="Sitecore.Data.Items" %>
<%@ Import Namespace="Sitecore.Globalization" %>
<%@ Import Namespace="Sitecore.SecurityModel" %>
<%@ Import Namespace="SitecoreData.DataProviders" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
    <head>
        <title>Transfer items between databases</title>
    </head>
    <body>
        <script runat="server">

            protected override void OnLoad(EventArgs e)
            {
                if (!IsPostBack)
                {
                    ddlSourceDatabase.DataSource = new object[] {"select source"}.Concat(Factory.GetDatabaseNames());
                    ddlSourceDatabase.DataBind();

                    ddlTargetDatabase.DataSource = new object[] {"select target"}.Concat(Factory.GetDatabaseNames());
                    ddlTargetDatabase.DataBind();
                }
            }

            private void OnStartButtonClick(object sender, EventArgs e)
            {
                if (!"select source".Equals(ddlSourceDatabase.SelectedValue) && !"select target".Equals(ddlTargetDatabase.SelectedValue))
                {
                    var sourceDatabase = Factory.GetDatabase(ddlSourceDatabase.SelectedValue);
                    var targetDatabase = Factory.GetDatabase(ddlTargetDatabase.SelectedValue);

                    if (sourceDatabase != null && targetDatabase != null)
                    {
                        using (new SecurityDisabler())
                        {
                            var item = sourceDatabase.GetRootItem();

                            var dataProvider = targetDatabase.GetDataProviders().First() as DataProviderWrapper;

                            Response.Write("<ul>");
                            Response.Flush();

                            TransferUtil.TransferItemAndDescendants(item, dataProvider, this.ReportItemCreated);

                            Response.Write("</ul>");
                            Response.Flush();
                        }
                    }
                }
            }

            private void ReportItemCreated(string path)
            {
                Response.Write(string.Format("<li>Transferring {0}</li>", path));
                Response.Flush();
            }
           

</script>
        <form runat="server">
            <p>
                Transfer items from  <asp:DropDownList runat="server" ID="ddlSourceDatabase" /> to <asp:DropDownList runat="server" ID="ddlTargetDatabase"  /> <asp:Button runat="server" ID="btnStart" OnClick="OnStartButtonClick" Text="Start" />
            </p>
        </form>
    </body>
</html>