using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace TrialMod
{
    public class KorexTankModule : PartModule
    {
        private bool hasKorex;
        PartResource Korex;
        int KorexId;
        //maximum heat to dispurse to around parts (closest part heat if max korex)
        const int maxHeatExplosion = 50;
        //OnStart
        public override void OnStart(StartState state)
        {
            //Get korex ID
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition("Korex");
            KorexId = resource.id;
        }
        //OnUpdate
        public override void OnUpdate()
        {
            //Get Korex
            Korex = part.Resources.Get(KorexId);

            //check if has korex
            if (Korex.amount > 0)
            {
                Debug.Log("Has Korex:" + Korex.amount + " units onboard");
                hasKorex = true;
            }
            //If has korex
            if (hasKorex)
            {
                //Use EC
                if (part.RequestResource("ElectricCharge", Korex.amount / 100000 * TimeWarp.CurrentRate) == 0)
                {
                    //If no EC
                    Debug.Log("NO ELECTRIC!!!!!!!!!!!!!!!!!!!!1111!! >:(");
                    //Heat part
                    part.temperature += 3 * TimeWarp.CurrentRate;
                }
            }
            //If part close to explosion, heat parts around
            if (part.temperature > part.maxTemp - 2)
            {
                //Get part number
                int index = vessel.Parts.IndexOf(part) + 1;
                //setup index
                int i = 1;
                //For each part
                foreach (Part p in vessel.Parts)
                {
                    //check how close
                    int howClose = Math.Abs(index - i);
                    //if not this part
                    if (howClose > 0)
                    {
                        /*
                         * TODO: Fix this code
                        //Add KorexSpillModule to contaminate parts
                        if (!p.Modules.Contains("KorexSpillModule"))
                        {
                            //Create new KorexSpill
                            KorexSpillModule kmod = new KorexSpillModule();
                            //Set variables for KorexSpill
                            kmod.KorexSpillAmount = Mathf.RoundToInt((float)(((1 - (howClose / (vessel.Parts.Count - index))) * (Korex.amount / Korex.maxAmount)) * 100));
                            //Create config from module
                            ConfigNode conf = ConfigNode.CreateConfigFromObject(kmod);
                            //Add KorexSpill to part
                            p.AddModule(conf);
                        }
                        else
                        {
                            //Get KorexSpill
                            KorexSpillModule pmod = p.Modules.GetModule<KorexSpillModule>();
                            //Add to KorexSpill
                            pmod.KorexSpillAmount += Mathf.RoundToInt((float)(((1 - (howClose / (vessel.Parts.Count - index))) * (Korex.amount / Korex.maxAmount)) * 100));
                        }
                        */
                        //heat part sepending on HowClose
                        p.temperature += maxHeatExplosion * (1 - (howClose / (vessel.Parts.Count - index))) * (Korex.amount / Korex.maxAmount);
                    }
                    i++;
                }
            }
        }
    }
    //Module added to parts that get korex spilled on them
    //Heats up parts at an increasing rate
    public class KorexSpillModule : PartModule
    {
        [KSPField(guiActive = true, guiName = "Korex Spill Amount")]
        public int KorexSpillAmount { get; set; } = 1;
        double counter = 1;
        //OnStart
        public override void OnStart(StartState state)
        {

        }

        public override void OnUpdate()
        {
            //Add part temperature at an increaing rate
            part.temperature += KorexSpillAmount * counter;
            //increase rate
            counter += 0.1;
        }
    }

    //TODO: Resume this
    public class KorexDeathRayModule : PartModule
    {
        //FIELDS
        //Ray power, 1 means explosion, 15 means MORE EXPLOSION
        [KSPField]
        public int rayPower = 15;
        //Power usage for precharge in EC/s
        [KSPField]
        public int rayPowerUsage = 50;
        //Power usage for fire in EC/fire
        [KSPField]
        public int rayFirePowerUsage = 300;
        //Ray fire range in meters
        [KSPField]
        public int fireRange = 10000000;
        //Ray Korex usage on precharge in Korex/s
        [KSPField]
        public double chargeFuelUsage = 0.0005;
        //Ray Korex usage on fire in Korex/fire
        [KSPField]
        public double fireFuelUsage = 5;

        //EVENTS
        [KSPEvent(guiActive = true, guiName = "Fire cannon")]
        public bool fireCannon()
        {
            ScreenMessages.PostScreenMessage("The precharge will not check if you have fuel, and just use it. Check to have 7.5 units of Korex and plenty of EC");
            //check if enough fuel
            if (part.RequestResource(korexID, chargeFuelUsage) == chargeFuelUsage)
            {
                //start charge coroutine
                StartCoroutine(prechargeThing());
            }
            return false;
        }

        //Coroutines
        //Precharge
        IEnumerator prechargeThing()
        {

            //charge for 5000ms
            for (int i = 0; i < 5000; i++)
            {
                //Check if fuel left
                if (part.RequestResource(korexID, chargeFuelUsage) < chargeFuelUsage)
                {
                    //Stop charge
                    ScreenMessages.PostScreenMessage("Not enough fuel to fire. Aborting...");
                    StopCoroutine("prechargeThing");
                }
                yield return new WaitForSeconds(0.001f);
            }
            chargeDone = true;
        }
        IEnumerator fireThing()
        {
            //get target
            ITargetable target = vessel.targetObject;
            Vessel attackVessel = CreateAttackVessel();
            yield return new WaitForSeconds(1);
        }

        public Vessel CreateAttackVessel()
        {
            // Create a new blank vessel
            Vessel newVessel = new Vessel();

            // Add a part to the vessel

            Debug.Log("getPartInfoByName");
            AvailablePart avParts = PartLoader.getPartInfoByName("fuelTank");

            Debug.Log("Instantiate");
            //UnityEngine.Object obj = UnityEngine.Object.Instantiate(avParts.partPrefab);
            if (avParts != null)
            {
                UnityEngine.Object obj = Instantiate(avParts.partPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
                Debug.Log("newPart");
                Part newPart = (Part)obj;
                Debug.Log("rootPart ; parts.Add()");
                newPart.gameObject.name = "fuelTank";
                newPart.partInfo = avParts;
                newVessel.rootPart = newPart;
                newVessel.parts = new List<Part>();
                newVessel.parts.Add(newPart);

                // Set vessel parameters
                Debug.Log("Set vessel params");
                Debug.Log("-----------------");


                Debug.Log("Set vessel name");
                if (newVessel != null)
                {
                    newVessel.name = "fuelTank";
                }
                else
                {
                    Debug.Log("Vessel is Empty");
                }



                Debug.Log("Set Landed status");
                if (newVessel != null)
                {
                    newVessel.Landed = true;
                }
                else
                {
                    Debug.Log("Vessel is Empty");
                }



                Debug.Log("Set Splashed status");
                if (newVessel != null)
                {
                    newVessel.Splashed = false;
                }
                else
                {
                    Debug.Log("Vessel is Empty");
                }



                Debug.Log("Set LandedAt status");
                if (newVessel != null)
                {
                    newVessel.landedAt = string.Empty;
                }
                else
                {
                    Debug.Log("Vessel is Empty");
                }



                Debug.Log("Set Landed status");
                if (newVessel != null)
                {
                    newVessel.situation = Vessel.Situations.LANDED;
                }
                else
                {
                    Debug.Log("Vessel is Empty");
                }


                Debug.Log("Set vessel type");
                if (newVessel != null)
                {
                    newVessel.vesselType = VesselType.Debris;
                }
                else
                {
                    Debug.Log("Vessel is Empty");
                }


                Debug.Log("Set vessel Position");
                if (newVessel != null)
                {
                    newVessel.transform.position = new Vector3(5f, 0f, 5f);
                }
                else
                {
                    Debug.Log("Vessel is Empty");
                }

                newVessel.GoOffRails();
                newVessel.Load();
            }
            else
            {
                print("Part Not Found");
            }
            return newVessel;
        }

        //Variables
        bool chargeDone = false;
        public int korexID;
        public override void OnStart(StartState state)
        {
            korexID = new PartResourceDefinition("Korex").id;
        }

    }


}
