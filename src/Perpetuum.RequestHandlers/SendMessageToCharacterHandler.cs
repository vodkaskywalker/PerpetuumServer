using Perpetuum.Host.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.RequestHandlers
{
    public class SendMessageToCharacterHandler : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var messageBuilder = Message.Builder.SetCommand(Commands.ServerMessage).WithData(new Dictionary<string, object>(request.Data));
            Message.Builder.FromRequest(request).WithOk().Send();

            Message.Builder
                .SetCommand(Commands.ServerMessage)
                .WithData(new Dictionary<string, object>(request.Data))
                .ToCharacter(request.Session.Character)
                .Send();
        }
    }
}
