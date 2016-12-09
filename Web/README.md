Getting started:

  1. In Web project, copy AppSettings.template.config and ConnectionStrings.Template.config to AppSettings.config and ConnectionStrings.config.
  2. Create an Azure AD application and register in AppSettings.  TODO
  3. Configure ConnectionStrings + AppSetttings for Mongo (install if needed).  TODO\
  4. Go to /admin/home - you should be prompted to log in and then get a "403.0 - Not an Admin" error from IIS.  Make sure your username is displayed properly - you'll need it later.
  5. Connect to the mongo DB you indicated - a globaladministrators collection should have been created with a record for the username "REPLACE THIS WITH YOUR USERNAME".  Edit (or replace) the record to put your username in from the previous step.
  6. Using the admin section (/admin), create an organization, building, floor, and room.
  7. Manually add a record to the organizationserviceconfiguration collection in Mongo that includes your Organization ID and supplies the parameters to connect to Exchange (yes, this will get a UI at some point).  It should look kinda like this (you may need to disable impersonation and change notification if using a non-admin account):
```{
    "_id" : ObjectId("....."),
    "OrganizationId" : ObjectId("....."),
    "ServiceName" : "Exchange",
    "Parameters" : {
        "Username" : ".....",
        "Password" : "......",
        "ServiceUrl" : "https://outlook.office365.com/EWS/Exchange.asmx",
        "IgnoreFree" : false,
        "UseChangeNotification" : true,
        "ImpersonateForAllCalls" : true,
        "EmailDomains" : [ 
            "......"
        ]
    }
}```