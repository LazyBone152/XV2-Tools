namespace LB_Mod_Installer.Binding
{
    public class AliasValue
    {
        public string Alias { get; set; }
        public string ID { get; set; }

        public AliasValue(string alias, string id)
        {
            Alias = alias;
            ID = id;
        }
    }


}
