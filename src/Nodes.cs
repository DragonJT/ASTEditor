using Raylib_cs;

class Function(Class returnType, Node parent) : Node(parent)
{
    public Class returnType = returnType;
    public TextBox name = new();

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText(returnType.name.text, Color.Blue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.Lime);
        nodeTree.DrawText("()", Color.Black);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }

    public static void CreateFunction(Window menu, Node node, NodeTree nodeTree, Window window)
    {
        var classes = node.Root.DescendingNodes().OfType<Class>();
        foreach(var c in classes)
        {
            menu.Add(new Button(c.name.text, () =>
            {
                var f = new Function(c, node);
                node.children.Add(f);
                nodeTree.selected = f;
                f.Input(window, nodeTree);

                var form = new Window(window, new Rectangle(100, 100, 400, 400));
                form.Add(f.name);
                f.name.onEnter = () =>
                {
                    form.Delete();
                };
            }));
        }
    }

    public override void Input(Window window, NodeTree nodeTree)
    {
        //var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        //CreateModule(menu, this, nodeTree, window);
    }
}

class Class(Node parent) : Node(parent)
{
    public TextBox name = new();

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("class", Color.SkyBlue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.Blue);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }

    public static void CreateClass(Window menu, Node node, NodeTree nodeTree, Window window)
    {
        menu.Add(new Button("class", () =>
        {
            var c = new Class(node);
            node.children.Add(c);
            nodeTree.selected = c;
            c.Input(window, nodeTree);

            var form = new Window(window, new Rectangle(100, 100, 400, 400));
            form.Add(c.name);
            c.name.onEnter = () =>
            {
                form.Delete();
            };
        }));
    }

    public override void Input(Window window, NodeTree nodeTree)
    {
        var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        Function.CreateFunction(menu, this, nodeTree, window);
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

    public static void CreateModule(Window menu, Node node, NodeTree nodeTree, Window window)
    {
        menu.Add(new Button("module", () =>
        {
            var m = new Module(node);
            node.children.Add(m);
            nodeTree.selected = m;
            m.Input(window, nodeTree);

            var form = new Window(window, new Rectangle(100, 100, 400, 400));
            form.Add(m.name);
            m.name.onEnter = () =>
            {
                form.Delete();
            };
        }));
    }

    public override void Input(Window window, NodeTree nodeTree)
    {
        var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        CreateModule(menu, this, nodeTree, window);
        Class.CreateClass(menu, this, nodeTree, window);
        Function.CreateFunction(menu, this, nodeTree, window);
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
    
    public override void Input(Window window, NodeTree nodeTree)
    {
        var menu = new Window(window, new Rectangle(100, 100, 400, 400));
        Module.CreateModule(menu, this, nodeTree, window);
    }
}
