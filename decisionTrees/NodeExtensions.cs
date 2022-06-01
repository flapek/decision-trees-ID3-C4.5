namespace decisionTrees;

public static class NodeExtensions
{
    public static async Task BuildTree(this Node node)
    {
        var decisions = node.Data.Select(x => x.Last()).GroupBy(x => x).Select(x => x.Key).ToArray();
        var (idx, ratio) = await node.Data.FindTheBest();
        if (ratio != 0)
        {
            node.Attribute = idx;
            var sort = await node.Data.Sort(idx);
            foreach (var child in sort.Select(d => new Node(d) {Value = d[0][idx]}))
            {
                node.Nodes.Add(child);
                await BuildTree(child);
            }
        }
        else
            node.Decision = decisions[0];
    }

// ReSharper disable once UnusedLocalFunction
    public static async Task DisplayTree(this Node tree, int tabs)
    {
        if (tree.Nodes.Count != 0)
        {
            ++tabs;
            Console.Write($"Attribute: {tree.Attribute}");
            foreach (var child in tree.Nodes)
            {
                Console.Write('\n');
                for (var i = 0; i < tabs; i++) Console.Write('\t');
                Console.Write($"{child.Value} -> ");

                await DisplayTree(child, ++tabs);
            }

            --tabs;
        }
        else
            Console.Write($"Decision: {tree.Decision}");
    }
}