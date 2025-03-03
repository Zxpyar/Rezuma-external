using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace basic_esp
{
    public class Offsets
    {
        //offsets.cs ändern sich immer nach update
        public static int dwEntityList = 0x1A369E0;
        public static int dwViewMatrix = 0x1AA27D0;
        public static int dwLocalPlayerPawn = 0x188AF10;
        public static int dwViewAngles = 0x1AA27D0;
        public static int dwGameRules = 0x1A9C800;

        //client.dll.cc ändert nur manchmal mit updates
        public static int m_vOldOrigin = 0x1324;
        public static int m_iTeamNum = 0x3E3;
        public static int m_lifeState = 0x348;
        public static int m_hPlayerPawn = 0x80C;
        public static int m_vecViewOffset = 0xCB0;
        public static int m_iHealth = 0x344;
        public static int m_iszPlayerName = 0x660;
        public static int m_pGameSceneNode = 0x328;
        public static int m_modelState = 0x170;
        public static int m_iIDEntIndex = 0x1458;
        public static int m_bBombPlanted = 0x9A5;
        public static int m_pClippingWeapon = 0x13A0;
        public static int m_iItemDefinitionIndex = 0x1BA;
        public static int m_AttributeManager = 0x1148;
        public static int m_Item = 0x50;

    }
}
