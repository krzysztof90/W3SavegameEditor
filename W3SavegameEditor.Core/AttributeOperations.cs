namespace W3SavegameEditor.Core
{
    public static class AttributeOperations
    {
        public static T GetClassAttribute<A, T>(Type type, Func<A, T> predicate) where A : Attribute
        {
            A[] attributes = (A[])type.GetCustomAttributes(typeof(A), false);

            if (attributes != null && attributes.Length != 0)
                return predicate(attributes[0]);
            return default(T);
        }

        public static Type GetTypeByAttribute<T, A>(Func<A, bool> predicate) where A : Attribute
        {
            return Assembly.GetAssembly(typeof(T)).GetTypes().Where(t => !t.IsAbstract && typeof(T).IsAssignableFrom(t))
                .SingleOrDefault(r => predicate(r.GetCustomAttributes(typeof(A), false).SingleOrDefault() as A));
        }
    }
}
