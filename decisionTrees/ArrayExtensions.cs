internal static class ArrayExtensions
{
    public static void Display(this List<object[]> array)
    {
        foreach (var item in array)
        {
            for (int j = 0; j < item.Length; j++)
                Console.Write("{0}(type: {1}) ", item[j], item[j].GetType());
            Console.WriteLine();
        }
    }

    public static void Display(this List<Attribute> attributes)
    {
        foreach (var item in attributes)
        {
            foreach (var value in item.Classes)
                Console.Write("{0}. {1}-{2} ", item.Index, value.Key, value.Value);
            Console.WriteLine("{0}. Entropy {1}, InformationValue {2}", item.Index, item.Entropy, item.InformationValue);
            Console.WriteLine();
        }
    }
}
