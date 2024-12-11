namespace Rise.Shared.Mailer
{
    public class EmailDto
    {
        public class Mutate
        {
            public string To { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
        }

        public class MutateMultiple
        {
            public List<string> Tos { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
        }
    }
}
