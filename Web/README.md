Getting started:

  1. In Web project, copy AppSettings.template.config and ConnectionStrings.Template.config to AppSettings.config and ConnectionStrings.config.
  2. Create an Azure AD application and register in AppSettings.  TODO
  3. Configure ConnectionStrings + AppSetttings for Mongo (install if needed).  TODO\
  4. Go to /admin/home - you should be prompted to log in and then get a "403.0 - Not an Admin" error from IIS.  Make sure your username is displayed properly - you'll need it later.
  5. Connect to the mongo DB you indicated - a globaladministrators collection should have been created with a record for the username "REPLACE THIS WITH YOUR USERNAME".  Edit (or replace) the record to put your username in from the previous step.
