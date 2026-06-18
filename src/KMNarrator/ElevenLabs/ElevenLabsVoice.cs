namespace KMNarrator.ElevenLabs
{
    public sealed class ElevenLabsVoice
    {
        public ElevenLabsVoice(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }

        public string Name { get; }
    }
}
