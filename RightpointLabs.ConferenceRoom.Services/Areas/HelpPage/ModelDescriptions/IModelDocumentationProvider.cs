using System;
using System.Reflection;

namespace RightpointLabs.ConferenceRoom.Services.Areas.HelpPage.ModelDescriptions
{
    public interface IModelDocumentationProvider
    {
        string GetDocumentation(MemberInfo member);

        string GetDocumentation(Type type);
    }
}