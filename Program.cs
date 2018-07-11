using System;
using System.Linq;
using System.Threading;
using vkchatstat.Types;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;



namespace vkchatstat
{
    class Program
    {
        static Token t;
        static RequestSender sender;


        static List<Conversation> GetConvs(string name, int max)
        {
            Result<ResponseArray<Conversation>> result = sender.Send<ResponseArray<Conversation>>(new ApiRequest($"messages.searchConversations?q={name}&offcount={max}"));
                
            if(result.IsError())
                throw new Exception(result.Error.ErrorMsg);

            return result.Response.Items.Where(c => c.Peer.Type == "chat").ToList();
        }


        static Conversation SelectChat(List<Conversation> convs)
        {
            Console.WriteLine("-1. exit");
            for(int i = 0; i < convs.Count; i++)
                    Console.WriteLine($"{i}. {convs[i].ChatSettings.Title}");

            int num = -2;
            while(true)
            {
                Console.Write("Choose your chat: ");

                if(!int.TryParse(Console.ReadLine(), out num) || num < -1 || num >= convs.Count) 
                    Console.WriteLine("Stop joking with me, it's wrong input");
                else
                    break;
            }

            if(num == -1)
                return null;

            //num = 0;
            return convs[num];
        }


        static Dictionary<long, string> GetChatMembers(Conversation chat)
        {  
            Result<ChatMembersResponse> result = sender.Send<ChatMembersResponse>(new ApiRequest($"messages.getConversationMembers?peer_id={chat.Peer.Id}"));
            if(result.IsError())
                throw new Exception(result.Error.ErrorMsg);

            Dictionary<long, string> members = new Dictionary<long, string>();
            foreach(Profile pr in result.Response.Profiles)
                members[pr.Id] =  $"{pr.FirstName} {pr.LastName}";

            return members; 
        }


        static List<Message> GetMessages(Conversation chat)
        {
            List<Message> msgs = new List<Message>();
            int offset = 0;

            while(true)
            {
                Thread.Sleep(500);
                //Result<ResponseArray<Message>> result = sender.Send<ResponseArray<Message>>(new ApiRequest($"execute.getChatMessages?peer_id={chat.Peer.Id}&rev=1&count=200&offset={offset}"));
                Result<ResponseArray<Message>> result = sender.Send<ResponseArray<Message>>(new ApiRequest($"messages.getHistory?peer_id={chat.Peer.Id}&rev=1&count=200&offset={offset}"));

                if(result.IsError())
                    continue;

                if(result.Response.Items.Count == 0)
                    break;

                msgs.AddRange(result.Response.Items);
                offset = msgs.Count();
                Console.WriteLine(offset);
            }

            return msgs;
        }


        static Dictionary<long, List<Message>> CountMessages(List<Message> msgs)
        {
            Dictionary<long, List<Message>> dic = new Dictionary<long, List<Message>>();

            foreach(Message msg in msgs)
            {
                if(!dic.ContainsKey(msg.FromId))
                    dic[msg.FromId] = new List<Message>();

                dic[msg.FromId].Add(msg);
            }

            return dic;
        }


        static Dictionary<long, List<Attachment>> CountPhotos(List<Message> msgs)
        {         
            List<Message> attach_msgs = msgs.Where(m => m.Attachments != null).ToList();
            Dictionary<long, List<Attachment>> dic = new Dictionary<long, List<Attachment>>();

            foreach(Message msg in attach_msgs)
            {
                if(!dic.ContainsKey(msg.FromId))
                    dic[msg.FromId] = new List<Attachment>();

                dic[msg.FromId].AddRange(msg.Attachments.Where(a => a.Photo != null).ToList());
            }   

            return dic;
        }


        static string StatToString(List<Statistic> stats, string title)
        {
            int num = 0;
            string str = "";

            foreach(Statistic stat in stats)
            {
                num += stat.stat_count;
                str += $"{stat.name}: {stat.stat_count}\n";
            }

            return $"{title} at all: {num}\n" + str;
        }


        static string ParseChatStatistic(Conversation chat)
        {
            Dictionary<long, string> members = GetChatMembers(chat);
            List<Message> msgs_all = GetMessages(chat);
            Dictionary<long, List<Message>> msgs = CountMessages(msgs_all);
            Dictionary<long, List<Attachment>> photos = CountPhotos(msgs_all);

            List<Statistic> msgs_stat = new List<Statistic>();
            foreach(long id in msgs.Keys)
                if(members.ContainsKey(id) && msgs.ContainsKey(id))
                    msgs_stat.Add(new Statistic(members[id], id, msgs[id].Count));
            msgs_stat = msgs_stat.OrderBy(st => -st.stat_count).ToList();
                    
            List<Statistic> photos_stat = new List<Statistic>();
            foreach(long id in photos.Keys)
                if(members.ContainsKey(id) && photos.ContainsKey(id))
                    photos_stat.Add(new Statistic(members[id], id, photos[id].Count));
            photos_stat = photos_stat.OrderBy(st => -st.stat_count).ToList();

            return $"statistic for {chat.ChatSettings.Title}\n\n{StatToString(msgs_stat, "messages")}\n{StatToString(photos_stat, "photos")}";
        }


        static void Main(string[] args)
        {
            string login = "";
            string pass = "";
            string chat_title = "";
            int max_chats = 10000;

            //авторизуемся
            Console.Write("Enter vk login: ");
            login = Console.ReadLine();
            Console.Write("Enter password: ");
            pass = Console.ReadLine();

            try
            {
                t = Token.Auth(login, pass, 274556);
                sender = new RequestSender(t, 3);
                Console.WriteLine(t.value);
            }
            catch
            {
                Console.WriteLine("error in authentification (maybe wrong pass or login)\n" +
                "https://oauth.vk.com/authorize?client_id=5635484&redirect_uri=https://oauth.vk.com/blank.html&scope={scope}&response_type=token&v=5.80&display=wap");
                Console.ReadKey();
                return;
            }

            //Ищем чаты
            Conversation chat;
            while(true)
            {
                Console.Write("Enter your chat title: ");
                chat_title = Console.ReadLine();

                List<Conversation> convs;
                try
                {
                    convs = GetConvs(chat_title, max_chats);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error while getting your shitty chats:\n{ex.Message}");
                    return;
                }

                if(convs.Count != 0)
                {
                    chat = SelectChat(convs);
                    if(chat != null)
                        break;
                }
                
                Console.WriteLine("There is 0 chats with name like that, please try again");
            }

            if(chat != null)
                Console.WriteLine(ParseChatStatistic(chat));
            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }
    }
}