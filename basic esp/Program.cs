using basic_esp;
using Swed64;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using WindowsInput;
using System.Timers;
using System.Diagnostics;


// main class

//init swed
Swed swed = new Swed("cs2");
IntPtr client = swed.GetModuleBase("client.dll");

var sim = new InputSimulator();

bool bombPlanted = false;

// init render 
Renderer renderer = new Renderer();
Thread renderthread = new Thread(new ThreadStart(renderer.Start().Wait));
renderthread.Start();

// get screen size from renderrer 
Vector2 screenSize = renderer.screensize;

//store entiteis 
List<Entity> entities = new List<Entity>();
Entity localplayer = new Entity();
Vector2 screen = new Vector2(1920, 1080); //for bones lol 

// esp loopen

while (true)
{
    entities.Clear();

    IntPtr entitylist = swed.ReadPointer(client, Offsets.dwEntityList);

    IntPtr gameRules = swed.ReadPointer(client, Offsets.dwGameRules);
    if (gameRules != IntPtr.Zero && renderer.bombTimer)
    {
        bombPlanted = swed.ReadBool(gameRules, Offsets.m_bBombPlanted);

        if (bombPlanted)
        {
            for (int i = 0; i < 40; i++)
            {
                bombPlanted = swed.ReadBool(gameRules, Offsets.m_bBombPlanted);
                if (!bombPlanted)
                    break;

                int timeLeft = 40 - i;

                renderer.timeLeft = timeLeft;
                renderer.bombPlanted = true;

                //Thread.Sleep(1000);
            }
        }
        else
        {
            renderer.timeLeft = -1;
            renderer.bombPlanted = false;
        }
    }
    //firts

    IntPtr listEntry = swed.ReadPointer(entitylist, 0x10);

    // local player get
    IntPtr localplayerpawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);

    localplayer.pawnAddress = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
    localplayer.origin = swed.ReadVec(localplayer.pawnAddress, Offsets.m_vOldOrigin);
    localplayer.team = swed.ReadInt(localplayerpawn, Offsets.m_iTeamNum);
    localplayer.view = swed.ReadVec(localplayer.pawnAddress, Offsets.m_vecViewOffset);

    IntPtr entityList = swed.ReadPointer(client, Offsets.dwEntityList);
    IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);

    //triggerbot anfang
    if (renderer.tbcombat)
    {
        int team = swed.ReadInt(localPlayerPawn, Offsets.m_iTeamNum);
        int entIndex = swed.ReadInt(localPlayerPawn, Offsets.m_iIDEntIndex);

        //Console.WriteLine($"Entity ID: {entIndex}");

        if (entIndex != -1)
        {
            IntPtr listEntry1 = swed.ReadPointer(entityList, 0x8 * ((entIndex & 0x7FFF) >> 9) + 0x10);

            IntPtr currentPawn = swed.ReadPointer(listEntry, 0x78 * (entIndex & 0x1FF));

            int entityTeam = swed.ReadInt(currentPawn, Offsets.m_iTeamNum);

            if (team != entityTeam)
            {
                if (GetAsyncKeyState(vKey.HOTKEY) < 0)
                {
                    sim.Mouse.LeftButtonClick();

                }
            }
        }
    }

    //triggerbot ende

    // loop throught 
    for (int i = 0; i < 64; i++)
    {

        if (listEntry == IntPtr.Zero)
            continue;

        IntPtr currentcontroller = swed.ReadPointer(listEntry, i * 0x78);

        if (currentcontroller == IntPtr.Zero) continue;

        int pawnHandle = swed.ReadInt(currentcontroller, Offsets.m_hPlayerPawn);
        if (pawnHandle == 0) continue;

        IntPtr listentry2 = swed.ReadPointer(entitylist, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
        if (listentry2 == IntPtr.Zero) continue;

        // get current pawn 
        IntPtr currentPawn = swed.ReadPointer(listentry2, 0x78 * (pawnHandle & 0x1FF));
        if (currentPawn == localplayerpawn) continue;

        IntPtr sceneNode = swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode);
        IntPtr boneMatrix = swed.ReadPointer(sceneNode, Offsets.m_modelState + 0x80);
        //int team = swed.ReadInt(currentPawn, m_iTeamNum);
        uint lifestate2 = swed.ReadUInt(currentPawn, Offsets.m_lifeState);
        int health = swed.ReadInt(currentPawn, Offsets.m_iHealth);
        int team = swed.ReadInt(currentPawn, Offsets.m_iTeamNum);
        // check if gegner is am leben 
        int lifestate = swed.ReadInt(currentPawn, Offsets.m_lifeState);
        if (lifestate != 256) continue;

        if (lifestate2 != 256) continue;

        if (team == localplayer.team && !renderer.aimOnTeam)
            continue;

        IntPtr currentWeapon = swed.ReadPointer(currentPawn, Offsets.m_pClippingWeapon);

        short weaponDefinitionIndex = swed.ReadShort(currentWeapon, Offsets.m_AttributeManager + Offsets.m_Item + Offsets.m_iItemDefinitionIndex);

        if (weaponDefinitionIndex == -1) continue;

        ViewMatrix viewMatrix3 = new ViewMatrix();

        // get matrix 
        float[] viewMatrix = swed.ReadMatrix(client + Offsets.dwViewMatrix);

        Entity entity = new Entity();


        entity.name = swed.ReadString(currentcontroller, Offsets.m_iszPlayerName, 16).Split("\0")[0];
        entity.team = swed.ReadInt(currentPawn, Offsets.m_iTeamNum);
        if (localplayer.team == entity.team && !renderer.teamesp) continue;
        entity.health = swed.ReadInt(currentPawn, Offsets.m_iHealth);
        entity.position = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin);
        entity.viewoffset = swed.ReadVec(currentPawn, Offsets.m_vecViewOffset);
        entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
        entity.ViewPosition2D = Calculate.WorldToScreen(viewMatrix, Vector3.Add(entity.position, entity.viewoffset), screenSize);
        entity.bones = Calculate.ReadBones(boneMatrix, swed);
        entity.bones2d = Calculate.ReadBones2d(entity.bones, viewMatrix, screen);
        entity.distance = Vector3.Distance(entity.origin, localplayer.origin);
        entity.origin = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin);
        entity.head = swed.ReadVec(boneMatrix, 6 * 32);
        entity.view = swed.ReadVec(currentPawn, Offsets.m_vecViewOffset);
        entity.lifeState = lifestate2;
        entity.currentWeaponIndex = weaponDefinitionIndex;
        entity.currentWeaponName = Enum.GetName(typeof(Weapon), weaponDefinitionIndex);


        ViewMatrix viewMatrix1 = ReadMatrix(client + Offsets.dwViewMatrix);
        entity.head2d = Calculate.WorldToScreen1(viewMatrix1, entity.head, (int)screenSize.X, (int)screenSize.Y);

        entity.pixelDistance = Vector2.Distance(entity.head2d, new Vector2(screenSize.X / 2, screenSize.Y / 2));
        entities.Add(entity);
    }

    entities = entities.OrderBy(o => o.pixelDistance).ToList();

    if (entities.Count > 0 && GetAsyncKeyState(vKey.HOTKEYAIM) < 0 && renderer.aimbot)
    {
        Vector3 playerView = Vector3.Add(localplayer.origin, localplayer.view);
        Vector3 entityView = Vector3.Add(entities[0].origin, entities[0].view);

        if (entities[0].pixelDistance < renderer.FOV)
        {
            Vector2 newAngles = Calculate.CalculateAngles(playerView, entities[0].head);
            Vector3 newAnglesVec3 = new Vector3(newAngles.Y, newAngles.X, 0.0f);

            swed.WriteVec(client, Offsets.dwViewAngles, newAnglesVec3);
        }
    }
    //Thread.Sleep(10);

    // update renderer 
    renderer.UpdateLocalPlayer(localplayer);
    renderer.UpdateEntities(entities);


}

[DllImport("user32.dll")]
static extern short GetAsyncKeyState(int vKey);

ViewMatrix ReadMatrix(IntPtr matrixAddress)
{
    var viewMatrix = new ViewMatrix();
    var matrix = swed.ReadMatrix(matrixAddress);

    // first row
    viewMatrix.m11 = matrix[0];
    viewMatrix.m12 = matrix[1];
    viewMatrix.m13 = matrix[2];
    viewMatrix.m14 = matrix[3];

    // second
    viewMatrix.m21 = matrix[4];
    viewMatrix.m22 = matrix[5];
    viewMatrix.m23 = matrix[6];
    viewMatrix.m24 = matrix[7];

    viewMatrix.m31 = matrix[8];
    viewMatrix.m32 = matrix[9];
    viewMatrix.m33 = matrix[10];
    viewMatrix.m34 = matrix[11];

    // fourth
    viewMatrix.m41 = matrix[12];
    viewMatrix.m42 = matrix[13];
    viewMatrix.m43 = matrix[14];
    viewMatrix.m44 = matrix[15];

    return viewMatrix;
}
