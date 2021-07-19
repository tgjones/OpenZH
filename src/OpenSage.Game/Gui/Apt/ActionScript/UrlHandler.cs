using System.IO;
using OpenSage.Gui.Apt.ActionScript.Library;

namespace OpenSage.Gui.Apt.ActionScript
{
    /// <summary>
    /// Url handler
    /// </summary>
    public static class UrlHandler
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static void Handle(VM.HandleCommand cmdHandler, VM.HandleExternalMovie movieHandler, ActionContext context, string url, string target)
        {
            logger.Debug($"[URL] URL: {url} Target: {target}");

            if (url.StartsWith("FSCommand:"))
            {
                var command = url.Replace("FSCommand:", "");

                cmdHandler(context, command, target);
            }
            else
            {
                //DO STUFF
                var targetObject = context.This.Variables[target].ToObject<StageObject>();

                if (!(targetObject.Item is SpriteItem))
                {
                    logger.Error("[URL] Target must be a sprite!");
                }

                var targetSprite = targetObject.Item as SpriteItem;
                var aptFile = movieHandler(url);
                var oldName = targetSprite.Name;

                targetSprite.Create(aptFile.Movie, targetSprite.Context, targetSprite.Parent);
                targetSprite.Name = oldName;
            }
        }
    }
}
