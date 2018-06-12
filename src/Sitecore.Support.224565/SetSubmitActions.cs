using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.ContentEditor.Data;
using Sitecore.Forms.Core.Data;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Abstractions.Dependencies;
using System;
using System.Collections.Specialized;
using System.Web;

namespace Sitecore.Support.Forms.Core.Commands
{
  [Serializable]
  public class SetSubmitActions : Command
  {
    public override void Execute(CommandContext context)
    {
      Assert.ArgumentNotNull(context, "context");
      Error.AssertObject(context, "context");
      if (context.Items.Length == 1)
      {
        Item item = context.Items[0];
        NameValueCollection nameValueCollection = new NameValueCollection();
        nameValueCollection["id"] = item.ID.ToString();
        nameValueCollection["db"] = item.Database.Name;
        nameValueCollection["la"] = item.Language.Name;
        nameValueCollection["mode"] = context.Parameters["mode"];
        nameValueCollection["root"] = context.Parameters["root"];
        nameValueCollection["system"] = context.Parameters["system"];
        Context.ClientPage.Start(this, "Run", nameValueCollection);
      }
    }

    protected void Run(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args.Parameters["id"], "id");
      Assert.ArgumentNotNull(args.Parameters["db"], "db");
      if (!Language.TryParse(args.Parameters["la"], out Language language))
      {
        language = Language.Current;
      }
      //Item item = Database.GetItem(new ItemUri(ID.Parse(args.Parameters["id"]), Language.Current, Sitecore.Data.Version.Latest, args.Parameters["db"]));
      Item item = Database.GetItem(new ItemUri(ID.Parse(args.Parameters["id"]), language, Sitecore.Data.Version.Latest, args.Parameters["db"]));
      if (SheerResponse.CheckModified() && item != null)
      {
        if (args.IsPostBack)
        {
          UrlHandle urlHandle = UrlHandle.Get(new UrlString(args.Parameters["url"]));
          item.Editing.BeginEdit();
          item.Fields["__Tracking"].Value = urlHandle["tracking"];
          if (args.HasResult)
          {
            ListDefinition listDefinition = ListDefinition.Parse((args.Result == "-") ? string.Empty : args.Result);
            if (args.Parameters["mode"] == "save")
            {
              //item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveActionsID].Value = listDefinition.ToXml();
              item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveActionsID].Value = Sitecore.Form.Core.Utility.ActionUtil.GetGlobalSaveActions(item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveActionsID].Value, listDefinition.ToXml());
              item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.LocalizedSaveActionsID].Value = Sitecore.Form.Core.Utility.ActionUtil.GetLocalizedSaveActions(listDefinition.ToXml());
            }
            else
            {
              item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.CheckActionsID].Value = listDefinition.ToXml();
            }
          }
          item.Editing.EndEdit();
        }
        else
        {
          string text = ID.NewID.ToString();
          UrlString urlString = new UrlString(UIUtil.GetUri("control:SubmitCommands.Editor"));
          FormItem formItem = new FormItem(item);
          //ListDefinition value = ListDefinition.Parse((args.Parameters["mode"] == "save") ? formItem.SaveActions : formItem.CheckActions);
          ListDefinition value = ListDefinition.Parse((args.Parameters["mode"] == "save") ? Sitecore.Form.Core.Utility.ActionUtil.OverrideParameters(formItem.SaveActions, formItem.LocalizedSaveActions) : formItem.CheckActions);
          HttpContext.Current.Session.Add(text, value);
          urlString.Append("definition", text);
          urlString.Add("id", args.Parameters["id"]);
          urlString.Add("db", args.Parameters["db"]);
          urlString.Add("la", args.Parameters["la"]);
          urlString.Append("root", args.Parameters["root"]);
          urlString.Append("system", args.Parameters["system"] ?? string.Empty);
          args.Parameters.Add("params", text);
          UrlHandle urlHandle2 = new UrlHandle();
          urlHandle2["title"] = DependenciesManager.ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SELECT_SAVE_TITLE" : "SELECT_CHECK_TITLE");
          urlHandle2["desc"] = DependenciesManager.ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SELECT_SAVE_DESC" : "SELECT_CHECK_DESC");
          urlHandle2["actions"] = DependenciesManager.ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SAVE_ACTIONS" : "CHECK_ACTIONS");
          urlHandle2["addedactions"] = DependenciesManager.ResourceManager.Localize((args.Parameters["mode"] == "save") ? "ADDED_SAVE_ACTIONS" : "ADDED_CHECK_ACTIONS");
          urlHandle2["tracking"] = formItem.Tracking.ToString();
          urlHandle2.Add(urlString);
          args.Parameters["url"] = urlString.ToString();
          Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
          args.WaitForPostBack();
        }
      }
    }
  }
}