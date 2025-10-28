using Raylib_cs;

class Function(IType returnType, Node parent) : Node(parent)
{
    public IType returnType = returnType;
    public TextBox name = new();

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText(returnType.Name, Color.Blue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.Lime);
        nodeTree.DrawText("()", Color.Black);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }

    public static void CreateFunction(List<Search> searches, Node node, NodeTree nodeTree, Window window)
    {
        var classes = node.Root.DescendingNodes().OfType<Class>();
        IType[] types = [.. classes, .. Program.primitives];
        foreach (var t in types)
        {
            searches.Add(new Search(t.Name, () =>
            {
                var f = new Function(t, node);
                node.children.Add(f);
                nodeTree.selected = f;
                f.Input(window, nodeTree, "");

                var form = new Window(window, new Rectangle(100, 100, 400, 400));
                form.Add(f.name);
                f.name.onEnter = () =>
                {
                    form.Delete();
                };
            }));
        }
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {
        //var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        //CreateModule(menu, this, nodeTree, window);
    }
}

class Class(Node parent) : Node(parent), IType
{
    public TextBox name = new();
    public string Name => name.text;

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("class", Color.SkyBlue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.Blue);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }

    public static void CreateClass(List<Search> searches, Node node, NodeTree nodeTree, Window window)
    {
        searches.Add(new Search("class", () =>
        {
            var c = new Class(node);
            node.children.Add(c);
            nodeTree.selected = c;
            c.Input(window, nodeTree, "");

            var form = new Window(window, new Rectangle(100, 100, 400, 400));
            form.Add(c.name);
            c.name.onEnter = () =>
            {
                form.Delete();
            };
        }));
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {
        var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        var searches = new List<Search>();
        Function.CreateFunction(searches, this, nodeTree, window);
        menu.Add(new SearchBox(text, [.. searches]));
    }
}

class Module(Node parent) : Node(parent)
{
    public TextBox name = new();

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("module", Color.SkyBlue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.Blue);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }

    public static void CreateModule(List<Search> searches, Node node, NodeTree nodeTree, Window window)
    {
        searches.Add(new Search("module", () =>
        {
            var m = new Module(node);
            node.children.Add(m);
            nodeTree.selected = m;
            m.Input(window, nodeTree, "");

            var form = new Window(window, new Rectangle(100, 100, 400, 400));
            form.Add(m.name);
            m.name.onEnter = () =>
            {
                form.Delete();
            };
        }));
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {
        var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        var searches = new List<Search>();
        CreateModule(searches, this, nodeTree, window);
        Class.CreateClass(searches, this, nodeTree, window);
        Function.CreateFunction(searches, this, nodeTree, window);
        menu.Add(new SearchBox(text, [.. searches]));
    }
}

class Root : Node
{
    public Root() : base(null) { }

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("root", Color.SkyBlue);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }
    
    public override void Input(Window window, NodeTree nodeTree, string text)
    {
        var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        var searches = new List<Search>();
        Module.CreateModule(searches, this, nodeTree, window);
        menu.Add(new SearchBox(text, [.. searches]));
    }
}
