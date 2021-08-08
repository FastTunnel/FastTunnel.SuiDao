﻿using FastTunnel.Core.Client;
using FastTunnel.Core.Exceptions;
using FastTunnel.Core.Handlers.Server;
using FastTunnel.Core.Models;
using Microsoft.Extensions.Logging;
using SuiDao.Client.Models;
using SuiDao.Client;
using SuiDao.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using Yarp.ReverseProxy.Configuration;

namespace SuiDao.Server.Handlers
{
    public class SuiDaoLoginHandler : LoginHandler
    {
        public SuiDaoLoginHandler(ILogger<SuiDaoLoginHandler> logger, IProxyConfigProvider proxyConfig)
            : base(logger, proxyConfig)
        {
        }

        public override async Task<bool> HandlerMsg(FastTunnelServer server, WebSocket client, string content)
        {
            var logMsg = System.Text.Json.JsonSerializer.Deserialize<LogInByKeyMassage>(content);
            var res = HttpHelper.PostAsJsonAsync(SuiDaoApi.GetTunnelListByKeyAndServerId, $"{{ \"key\":\"{logMsg.key}\",\"server_id\":{logMsg.server_id}}}").Result;

            var jobj = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<Tunnel[]>>(res);
            if (jobj.success)
            {
                var tunnels = jobj.data;
                var Webs = new List<WebConfig>();
                var SSH = new List<ForwardConfig>();

                foreach (var tunnel in tunnels)
                {
                    if (tunnel.app_type == 1) // web
                    {
                        Webs.Add(new WebConfig
                        {
                            LocalIp = tunnel.local_ip,
                            LocalPort = tunnel.local_port,
                            SubDomain = tunnel.sub_domain
                        });
                    }
                    else if (tunnel.app_type == 2)
                    {
                        SSH.Add(new ForwardConfig
                        {
                            LocalIp = tunnel.local_ip,
                            LocalPort = tunnel.local_port,
                            RemotePort = tunnel.remote_port,
                        });
                    }
                }

                await HandleLoginAsync(server,client, new LogInMassage
                {
                    Forwards = SSH,
                    Webs = Webs,
                });

                return NeedRecive;
            }
            else
            {
                throw new APIErrorException(jobj.errorMsg);
            }
        }
    }
}