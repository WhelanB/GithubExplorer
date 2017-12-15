using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitHub.Controllers
{
    public class Graph
    {
        public string name;
        public string img;
        public string url;
        public List<Graph> children;
        public Graph(string n, string i, string u)
        {
            name = n;
            img = i;
            url = u;
        }

        public void AddChild(Graph g)
        {
            if (children == null)
                children = new List<Graph>();
            if(children.FindIndex(f => f.name == g.name) < 0)
                children.Add(g);
             
        }

    }
}
