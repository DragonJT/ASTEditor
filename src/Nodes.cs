using Raylib_cs;

class BinaryExpr(Node parent, string op) : Node(parent)
{
    public override bool Selectable => false;

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSpace();
        nodeTree.DrawText(op, Color.DarkGray);
        nodeTree.DrawSpace();
        if(children.Count == 1)
        {
            children[0].Draw(window, nodeTree);
        }
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {
    }

    public static void Create(string text, Window parentWindow, Node parentNode)
    {
        var searchBox = new SearchBox(text);
        var form = new Window(parentWindow, new Rectangle(100, 100, 400, 400), [searchBox]);
        string[] operators = ["+", "-", "*", "/"];
        foreach(var o in operators)
        {
            searchBox.Add(o, () =>
            {
                var binaryExpr = new BinaryExpr(parentNode, o);
                form.Delete();
                NumberExpr.Create("", parentWindow, binaryExpr);
            });
        }
    }
}

class NumberExpr(Node parent, string value) : Node(parent)
{
    public override bool Selectable => false;

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawText(value, Color.Magenta);
        if(children.Count == 1)
        {
            children[0].Draw(window, nodeTree);
        }
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {
    }

    public static void Create(string text, Window parentWindow, Node parentNode)
    {
        var sbox = new SearchBox(text);
        var form = new Window(parentWindow, new Rectangle(100, 100, 400, 400), [sbox]);
        sbox.onNumberInput = n =>
        {
            var numberExpr = new NumberExpr(parentNode, n);
            form.Delete();
            BinaryExpr.Create("", parentWindow, numberExpr);
        };
    }
}

class ImplicitVariableDecl(Node parent) : Node(parent)
{
    public TextBox name = new();

    public override bool Selectable => true;

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("var", Color.SkyBlue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.Lime);
        if (children.Count == 1)
        {
            nodeTree.DrawSpace();
            nodeTree.DrawText("=", Color.DarkGray);
            nodeTree.DrawSpace();
            children[0].Draw(window, nodeTree);
        }
        nodeTree.NewLine();
    }

    public static void Create(SearchBox searchBox, Node parentNode, NodeTree nodeTree, Window parentWindow)
    {
        searchBox.Add("var", () =>
        {
            var v = new ImplicitVariableDecl(parentNode);
            nodeTree.selected = v;
            var form = new Window(parentWindow, new Rectangle(100, 100, 400, 400), [v.name]);

            v.name.onEnter = () =>
            {
                NumberExpr.Create("", parentWindow, v);
            };
        });
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {

    }
}

class Function(IType returnType, Node parent) : Node(parent)
{
    public IType returnType = returnType;
    public TextBox name = new();

    public override bool Selectable => true;

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

    public static void Create(SearchBox searchBox, Node node, NodeTree nodeTree, Window window)
    {
        var classes = node.Root.DescendingNodes().OfType<Class>();
        IType[] types = [.. classes, .. Program.primitives];
        foreach (var t in types)
        {
            searchBox.Add(t.Name, () =>
            {
                var f = new Function(t, node);
                nodeTree.selected = f;

                var form = new Window(window, new Rectangle(100, 100, 400, 400), [f.name]);
                f.name.onEnter = () =>
                {
                    form.Delete();
                };
            });
        }
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {
        var searchBox = new SearchBox(text);
        ImplicitVariableDecl.Create(searchBox, this, nodeTree, window);
        new Window(window, new Rectangle(100, 100, 400, 400), [searchBox]);
    }
}

class Class(Node parent) : Node(parent), IType
{
    public TextBox name = new();
    public string Name => name.text;

    public override bool Selectable => true;

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("class", Color.SkyBlue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.Blue);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }

    public static void Create(SearchBox searchBox, Node node, NodeTree nodeTree, Window window)
    {
        searchBox.Add("class", () =>
        {
            var c = new Class(node);
            nodeTree.selected = c;

            var form = new Window(window, new Rectangle(100, 100, 400, 400), [c.name]);
            c.name.onEnter = () =>
            {
                form.Delete();
            };
        });
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {
        var searchBox = new SearchBox(text);
        Function.Create(searchBox, this, nodeTree, window);
        new Window(window, new Rectangle(100, 100, 400, 400), [searchBox]);

    }
}

class Module(Node parent) : Node(parent)
{
    public TextBox name = new();

    public override bool Selectable => true;

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("module", Color.SkyBlue);
        nodeTree.DrawSpace();
        nodeTree.DrawText(name.text, Color.Blue);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }

    public static void Create(SearchBox searchBox, Node node, NodeTree nodeTree, Window window)
    {
        searchBox.Add("module", () =>
        {
            var m = new Module(node);
            nodeTree.selected = m;
            
            var form = new Window(window, new Rectangle(100, 100, 400, 400), [m.name]);
            m.name.onEnter = () =>
            {
                form.Delete();
            };
        });
    }

    public override void Input(Window window, NodeTree nodeTree, string text)
    {
        var searchBox = new SearchBox(text);
        Create(searchBox, this, nodeTree, window);
        Class.Create(searchBox, this, nodeTree, window);
        Function.Create(searchBox, this, nodeTree, window);
        new Window(window, new Rectangle(100, 100, 400, 400),  [searchBox]);

    }
}

class Root : Node
{
    public Root() : base(null) { }

    public override bool Selectable => true;

    public override void Draw(Window window, NodeTree nodeTree)
    {
        nodeTree.DrawSelected(window, this);
        nodeTree.DrawText("root", Color.SkyBlue);
        nodeTree.NewLine();
        nodeTree.DrawChildren(window, children);
    }
    
    public override void Input(Window window, NodeTree nodeTree, string text)
    {
        var searchBox = new SearchBox(text);
        Module.Create(searchBox, this, nodeTree, window);
        new Window(window, new Rectangle(100, 100, 400, 400), [searchBox]);
    }
}
