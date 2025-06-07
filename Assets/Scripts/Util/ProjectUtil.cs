using System.Collections;
using Random = System.Random;

public static class ProjectUtil
{
    public static void ShuffleList(this IList list)
    {
        var random = new Random();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var randomIndex = random.Next(0, i + 1);
                
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
