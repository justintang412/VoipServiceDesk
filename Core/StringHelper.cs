using Arco.Models;
using Arco.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;

namespace Arco.Core
{
    public static class StringHelper
    {
        public static string ToMD5(this string str)
        {
            if (str == null) return null;
            byte[] bs = MD5.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
            string byte2String = null;

            for (int i = 0; i < bs.Length; i++)
            {
                byte2String += bs[i].ToString("x");
            }
            return byte2String;
        }

        public static string ToGMTZones(this string timestring, int zone)
        {
            IFormatProvider ifp = new CultureInfo("zh-CN", true);

            DateTime dt = DateTime.ParseExact(timestring, "yyyy-MM-dd HH:mm:ss", ifp);

            return dt.AddHours(zone).ToString("yyyy-MM-dd HH:mm:ss");
        }
        public static CallHistory ToCallHitory(this string callid)
        {
            List<BsonDocument> histories = DataService.GetInstance().Database.GetCollection<BsonDocument>("history").Find(new BsonDocument { { "callid", callid } }).ToList();

            if (histories != null && histories.Count > 0)
            {
                BsonDocument bsonElement = histories[0];
                CallHistory callHistory = new CallHistory();
                callHistory.Callid = callid;
                callHistory.Timestart = ((string)bsonElement["timestart"]);
                callHistory.Callfrom = (string)bsonElement["callfrom"];
                callHistory.Callto = (string)bsonElement["callto"];
                callHistory.Callduraction = (string)bsonElement["callduraction"];
                callHistory.Talkduraction = (string)bsonElement["talkduraction"];
                callHistory.Status = (string)bsonElement["status"];
                callHistory.Type = (string)bsonElement["type"];
                callHistory.Recording = (string)bsonElement["recording"];
                callHistory.Sn = (string)bsonElement["sn"];
                return callHistory;
            }


            return new CallHistory
            {
                Callid = callid
            };
        }

        public static string ToAgentStatus(this string type)
        {
            switch (type)
            {
                case "Ringing":
                    return "响铃";
                case "Busy":
                    return "通话中";
                case "Idle":
                    return "空闲";
                case "Registered":
                    return "已注册";
                case "Unregistered":
                    return "未注册";
                case "unavailable":
                    return "未注册";
            }
            return null;
        }

        public static string ToReadableGroupType(this string type)
        {
            switch (type)
            {
                case "paging":
                    return "单向传呼";
                case "intercom":
                    return "双向对讲";
                case "multicast":
                    return "单向组播";
            }
            return null;
        }
        public static string ToGroupType(this string type)
        {
            switch (type)
            {
                case "单向传呼":
                    return "paging";
                case "双向对讲":
                    return "intercom";
                case "单向组播":
                    return "multicast";
            }
            return null;
        }
        public static string ToCallStatus(this string status)
        {
            switch (status)
            {
                case "ALERT":
                    return "主叫回铃";
                case "RING":
                    return "振铃";
                case "ANSWERED":
                    return "主叫接听";
                case "ANSWER":
                    return "通话应答";
                case "HOLD":
                    return "保持";
                case "BYE":
                    return "挂断";
            }
            return "未知";
        }
        public static string ToMeetingStatus(this string status)
        {
            switch (status)
            {
                case "ANSWERED":
                    return "该成员拨打会议室号码，主动进⼊会议室";
                case "ANSWER":
                    return "该成员接受邀请，进⼊会议室";
                case "HOLD":
                    return "该成员已进⼊会议室，等待主持⼈进⼊会议室";
            }
            return "未知";
        }
        public static string ToCDRStatus(this string status)
        {
            switch (status)
            {
                case "ANSWERED":
                    return "被叫接听电话";
                case "NO ANSWER":
                    return "被叫未接听电话";
                case "BUSY":
                    return "被叫正在忙";
                case "VOICEMAIL":
                    return "被叫号码未接听，且有新的语⾳留⾔";
            }
            return "未知";
        }


        public static ContactWithSingleDevice SetupContact(this string number, MainWindowViewModel _viewModel)
        {
            ContactWithSingleDevice contactWithSingleDevice = new ContactWithSingleDevice();
            foreach (Contact _contact in _viewModel.BaseContacts)
            {
                foreach (Device _device in _contact.Devices)
                {
                    if (_device.Number.Equals(number))
                    {
                        contactWithSingleDevice.Contact = _contact;
                        contactWithSingleDevice.Device = _device;
                        return contactWithSingleDevice;
                    }
                }
            }
            Device device = new Device();
            contactWithSingleDevice.Device = device;
            return contactWithSingleDevice;
        }

        public static ContactWithSingleDevice SetupContactStatus(this string number, string transformedStatus, MainWindowViewModel _viewModel)
        {
            ContactWithSingleDevice contactWithSingleDevice = new ContactWithSingleDevice();
            foreach (Contact _contact in _viewModel.BaseContacts)
            {
                foreach (Device _device in _contact.Devices)
                {
                    if (_device.Number.Equals(number))
                    {
                        _device.Status = transformedStatus;
                        _contact.Status = transformedStatus;
                        contactWithSingleDevice.Contact = _contact;
                        contactWithSingleDevice.Device = _device;
                        return contactWithSingleDevice;
                    }
                }
            }
            Device device = new Device();
            contactWithSingleDevice.Device = device;
            return contactWithSingleDevice;
        }

        public static ContactWithSingleDevice ToContactWithSingleDevice(this string number, MainWindowViewModel _viewModel)
        {
            ContactWithSingleDevice contactWithSingleDevice = new ContactWithSingleDevice();
            foreach (Contact _contact in _viewModel.BaseContacts)
            {
                foreach (Device _device in _contact.Devices)
                {
                    if (_device.Number.Equals(number))
                    {
                        contactWithSingleDevice.Contact = _contact;
                        contactWithSingleDevice.Device = _device;
                        return contactWithSingleDevice;
                    }
                }
            }
            Device device = new Device();
            contactWithSingleDevice.Device = device;
            return contactWithSingleDevice;
        }
    }
}
