using System;

namespace WpfBotMessages
{
    [Serializable]
    public struct Message
    {
        public long Id { get; set; }

        public string FName { get; set; }

        public string Mess { get; set; }
        
        public Message(long id, string fName, string mess)
        {
            this.Id = id;
            this.FName = fName;
            this.Mess = mess;
        }
    }
}
