namespace PostgresPooling
{
    public class Foo
    {
        private static int index;

        public Foo()
        {
            Id = Guid.NewGuid();
            Index = index++;
        }

        public Guid Id { get; }
        public int Index { get; }
    }
}