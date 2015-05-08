using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Exchange.WebServices.Data;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    public class RoomController : ApiController
    {
        private static ExchangeService GetService()
        {
            var svc = new ExchangeService(ExchangeVersion.Exchange2010);
            svc.Credentials = new WebCredentials(ConfigurationManager.AppSettings["username"], ConfigurationManager.AppSettings["password"]);
            var serviceUrl = ConfigurationManager.AppSettings["serviceUrl"];
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                svc.Url = new Uri(serviceUrl);
            }
            else
            {
                svc.AutodiscoverUrl(ConfigurationManager.AppSettings["username"], url => new Uri(url).Scheme == "https");
            }
            return svc;
        }

        [HttpGet]
        public object Lists()
        {
            var svc = GetService();
            return svc.GetRoomLists().Select(i => new
            {
                i.Id,
                i.Address,
                i.MailboxType,
                i.Name,
                i.RoutingType
            }).ToList();
        }

        [HttpGet]
        public object Rooms(string address)
        {
            var svc = GetService();
            return svc.GetRooms(address).Select(i => new
            {
                i.Id,
                i.Address,
                i.MailboxType,
                i.Name,
                i.RoutingType
            }).ToList();
        }

        [HttpGet]
        public object Schedule(string roomAddress)
        {
            var svc = GetService();
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(svc, calId);
            return cal.FindAppointments(new CalendarView(DateTime.Today, DateTime.Today.AddDays(1))).Select(i => new
            {
                i.Id,
                i.Subject,
                i.Start,
                i.End,
                Organizer = i.Organizer.Name,
            }).ToList();
        }
    }
}
