
using System.Numerics;
using Raylib_cs;

static class RaylibHelper
{
    public static bool IsKeyPressed(KeyboardKey key)
    {
        return Raylib.IsKeyPressed(key) || Raylib.IsKeyPressedRepeat(key);
    }

    public static Rectangle Expand(this Rectangle rect, float radius)
    {
        return new Rectangle(rect.X - radius, rect.Y - radius, rect.Width + radius * 2, rect.Height + radius * 2);
    }
}

interface IDisplay
{
    void Display(NodeTree displayNodeTree, Node node);
}

class DisplayParameters(Color punctuationColor) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText("(", punctuationColor);
        for (var i = 0; i < node.children.Count; i++)
        {
            displayNodeTree.Draw(node.children[i]);
            if (i < node.children.Count - 1)
            {
                displayNodeTree.DrawText(",", punctuationColor);
                displayNodeTree.DrawSpace();
            }
        }
        displayNodeTree.DrawText(")", punctuationColor);
    }
}

class DisplayChildX(int x) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.Draw(node.children[x]);
    }
}

class DisplayValue(Color color) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText(node.value!, color);
    }
}

class DisplayNewLine : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.NewLine();
    }
}

class DisplaySpace : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawSpace();
    }
}

class DisplayType(Color color) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawText(node.nodeType.name, color);
    }
}

class DisplayChildren : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        displayNodeTree.DrawChildren(node);
    }
}

class DisplayChildrenOfChild(int childID) : IDisplay
{
    public void Display(NodeTree displayNodeTree, Node node)
    {
        if(childID < node.children.Count)
            displayNodeTree.DrawChildren(node.children[childID]);
    }
}

class DefaultTypes
{
    public NodeType[] types;
    public int id;

    public DefaultTypes(NodeType[] types, int id)
    {
        this.types = types;
        this.id = id;
    }
}

class NodeType
{
    public string name;
    public bool hasValue;
    public DefaultTypes? defaultTypes;
    public List<NodeType> subTypes = [];
    public List<IDisplay> display = [];

    public NodeType(string name, bool hasValue)
    {
        this.name = name;
        this.hasValue = hasValue;
    }

    public bool IsNewLineInTree => display.OfType<DisplayNewLine>().Any();
}

class Node
{
    public NodeType nodeType;
    public string? value;
    public Node? parent = null;
    public List<Node> children = [];

    public Node(Node? parent, NodeType nodeType, string? value)
    {
        this.parent = parent;
        this.nodeType = nodeType;
        this.value = value;
    }
}

class MenuItem
{
    public string name;
    public Action action;

    public MenuItem(string name, Action action)
    {
        this.name = name;
        this.action = action;
    }
}

class NodeTree : GUI
{
    Node root;
    Node selected;
    int x;
    int y;
    int depth;
    int fontSize = 50;
    int spaceSize;

    public NodeTree(GUIWindow guiWindow, Node root) : base(guiWindow)
    {
        this.root = root;
        selected = root;
    }
    
    public void DrawText(string text, Color color)
    {
        Raylib.DrawText(text, x + 2, y, fontSize, color);
        x += Raylib.MeasureText(text, fontSize) + 4;
    }

    public void DrawSpace()
    {
        x += spaceSize;
    }

    public void NewLine()
    {
        y += fontSize;
        x = 0;
    }

    public void DrawChildren(Node node)
    {
        depth++;
        foreach (var c in node.children)
        {
            x += spaceSize * 4 * depth;
            Draw(c);
        }

        depth--;
    }

    static Action CreateForm(GUIWindow guiWindow, Vector2 pos, int fontSize, Node node, NodeType nodeType)
    {
        if (nodeType.hasValue)
        {
            return () =>
            {
                var rect = new Rectangle(pos.X, pos.Y, 600, fontSize);
                var form = new Form(guiWindow, rect.Expand(50));
                var textBox = new TextBox(form, rect, fontSize);
                textBox.onEnter = () =>
                {
                    var newNode = new Node(node, nodeType, textBox.text);
                    if (nodeType.defaultTypes != null)
                    {
                        foreach (var n in nodeType.defaultTypes.types)
                        {
                            newNode.children.Add(new Node(newNode, n, "void"));
                        }
                    }
                    node.children.Add(newNode);
                    form.Delete();
                };
                form.Add(textBox);
            };
        }
        else
        {
            return () =>
            {
                var newNode = new Node(node, nodeType, null);
                node.children.Add(newNode);
                guiWindow.child = null;
            };
        }
    }

    static void CreateAddMenuItems(GUIWindow guiWindow, Menu menu, Vector2 pos, int fontSize, Node node)
    {
        var addNodeMenuItem = node.nodeType.subTypes
            .Select(t => new MenuItem($"Add {t.name}", CreateForm(guiWindow, pos, fontSize, node, t)))
            .ToArray();
        menu.menuItems.AddRange(addNodeMenuItem);
        
    }

    public void Draw(Node node)
    {
        var startY = y;
        if(selected == node)
        {
            Raylib.DrawRectangle(0, y, Raylib.GetScreenWidth(), fontSize, new Color(200,200,200));
        }
        foreach (var display in node.nodeType.display)
        {
            display.Display(this, node);
        }
        var m = new Vector2(100, startY + fontSize);
        if (Active && selected == node && RaylibHelper.IsKeyPressed(KeyboardKey.Enter))
        {
            var menu = new Menu(guiWindow, m, 400, fontSize);
            if (node.nodeType.defaultTypes == null)
            {
                CreateAddMenuItems(guiWindow, menu, m, fontSize, node);
            }
            else if (node.nodeType.defaultTypes.id >= 0)
            {
                CreateAddMenuItems(guiWindow, menu, m, fontSize, node.children[node.nodeType.defaultTypes.id]);
            }
            if (node.nodeType.hasValue)
            {
                menu.menuItems.Add(new MenuItem("Rename", () =>
                {
                    var rect = new Rectangle(m.X, m.Y, 600, fontSize);
                    var form = new Form(guiWindow, rect.Expand(50));
                    var textBox = new TextBox(form, rect, fontSize);
                    textBox.onEnter = () =>
                    {
                        node.value = textBox.text;
                        form.Delete();
                    };
                    form.Add(textBox);
                }));
            }
            if (node.parent != null)
            {
                menu.menuItems.Add(new MenuItem("Delete", () =>
                {
                    node.parent.children.Remove(node);
                    menu.Delete();
                }));
            }
        }
    }

    static void GetDescendingNodes(Node node, List<Node> nodes)
    {
        nodes.Add(node);
        foreach(var c in node.children)
        {
            GetDescendingNodes(c, nodes);
        }
    }

    static List<Node> GetDescendingNodes(Node root)
    {
        var nodes = new List<Node>();
        GetDescendingNodes(root, nodes);
        return nodes;
    }

    public override void Update()
    {
        x = 0;
        y = 0;
        depth = 0;
        spaceSize = Raylib.MeasureText(" ", fontSize);
        Draw(root);
        if (Active)
        {
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Up))
            {
                var nodes = GetDescendingNodes(root).Where(n=>n.nodeType.IsNewLineInTree).ToList();
                var id = nodes.IndexOf(selected);
                id--;
                if (id < 0)
                {
                    id = nodes.Count - 1;
                }
                selected = nodes[id];
            }
            if (RaylibHelper.IsKeyPressed(KeyboardKey.Down))
            {
                var nodes = GetDescendingNodes(root).Where(n=>n.nodeType.IsNewLineInTree).ToList();
                var id = nodes.IndexOf(selected);
                id++;
                if (id >= nodes.Count)
                {
                    id = 0;
                }
                selected = nodes[id];
            }
        }
    }
}

class Form : GUIWindow
{
    Rectangle rect;
    List<GUI> guis = [];

    public Form(GUIWindow? parent, Rectangle rect)
    {
        SetParent(parent);
        this.rect = rect;
    }

    public void Add(GUI gui)
    {
        guis.Add(gui);
    }

    public override void Update()
    {
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.White);
        foreach (var g in guis)
        {
            g.Update();
        }
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
        child?.Update();
        startFrame = false;
    }
}


class Program
{
    static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1000, 800, "ASTEditor");
        Raylib.MaximizeWindow();

        var form = new Form(null, new Rectangle(0,0,1000, 800));
        var exprType = new NodeType("expression", false);
        var typeType = new NodeType("type", true);
        var nameType = new NodeType("name", true);
        var paramsType = new NodeType("parameters", false);
        var bodyType = new NodeType("body", false);
        var funcType = new NodeType("function", true);
        var classType = new NodeType("class", true);
        var moduleType = new NodeType("module", true);
        var rootType = new NodeType("root", false);

        rootType.subTypes.Add(moduleType);
        moduleType.subTypes.AddRange([classType, funcType]);
        classType.subTypes.AddRange([funcType]);
        bodyType.subTypes.AddRange([exprType]);

        funcType.defaultTypes = new DefaultTypes([typeType, paramsType, bodyType], 2);
        paramsType.defaultTypes = new DefaultTypes([typeType, nameType], -1);
        typeType.display.Add(new DisplayValue(Color.Blue));

        paramsType.display.Add(new DisplayParameters(Color.DarkGray));

        exprType.display.AddRange([new DisplayType(Color.Magenta), new DisplayNewLine()]);

        rootType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplayNewLine(),
            new DisplayChildren()]);

        moduleType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplaySpace(),
            new DisplayValue(Color.DarkGreen),
            new DisplayNewLine(),
            new DisplayChildren()]);

        classType.display.AddRange([
            new DisplayType(Color.SkyBlue),
            new DisplaySpace(),
            new DisplayValue(Color.Blue),
            new DisplayNewLine(),
            new DisplayChildren()]);

        funcType.display.AddRange([
            new DisplayChildX(0),
            new DisplaySpace(),
            new DisplayValue(Color.DarkGreen),
            new DisplayChildX(1),
            new DisplayNewLine(),
            new DisplayChildrenOfChild(2),
        ]);
        var root = new Node(null, rootType, null);
        form.Add(new NodeTree(form, root));

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            form.Update();

            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}