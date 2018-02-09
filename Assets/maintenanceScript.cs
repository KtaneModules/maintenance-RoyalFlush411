using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class maintenanceScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMSelectable jobLeft;
    public KMSelectable jobRight;
    public KMSelectable repairBut;

    public KMAudio Audio;

    //Module text
    public TextMesh plate;
    public TextMesh jobText;
    public TextMesh notebookText;
    string date = DateTime.Today.ToString("m");

    //Lights
    public Renderer light1;
    public Renderer light2;
    public Renderer light3;
    public Renderer light4;
    public Texture onMat;
    public Texture offMat;

    //Job list data
    private int jobIndex;
    private List<string> jobEntries = new List<string> { "Wash", "Four tyres", "Windscreen chip", "Wiper replacement", "Two tyres", "Exhaust welding", "Headlight bulb", "Oil change", "One tyre", "Windscreen replacement", "Brake fluid change", "Head gasket replacement", "Write-off" };
    private List<string> jobsRequired = new List<string>();
    private List<string> jobsOrder = new List<string> { "Windscreen chip", "Brake fluid change", "Headlight bulb", "Wiper replacement", "Windscreen replacement", "Exhaust welding", "Head gasket replacement", "One tyre", "Two tyres", "Four tyres", "Oil change", "Wash" };
    private List<string> correctJobsOrder = new List<string>();

    //Plate info
    string plateAData;
    string plateBData;
    string plateCData;
    string plateData;

    //Car info
    int carModelMultiplier;
    string carModel;
    int carBaseValue;
    int carValue;
    string carYear;
    string insurance;
    int numberOfJobs;
    string plated;
    int jobsCost;
    string writeOff;

    //Job pricings
    int wash = 3;
    int headlightBulb = 6;
    int wiperReplacement = 10;
    int oilChange = 15;
    int brakeFluidChange = 25;
    int windscreenChip = 40;
    int oneTyre = 80;
    int windscreenReplacement = 150;
    int twoTyres = 160;
    int fourTyres = 320;
    int exhaustWelding = 500;
    int headGasketReplacement = 750;

    //Logging
    int stage = 1;
    static int moduleIdCounter = 1;
    int moduleId;

    void Awake()
    {
        //Activate buttons and jobs cycler
        moduleId = moduleIdCounter++;
        jobLeft.OnInteract += delegate () { OnjobLeft(); return false; };
        jobRight.OnInteract += delegate () { OnjobRight(); return false; };
        repairBut.OnInteract += delegate () { OnrepairBut(); return false; };
        jobIndex = UnityEngine.Random.Range(0, jobEntries.Count);
        jobText.text = jobEntries[jobIndex];
    }

    void Start()
    {
        //Build the number plate
        plateACollection();
        plateBCollection();
        plateCCollection();
        plateCCollection();
        plateCCollection();

        plateData = plateAData + plateBData + plateCData;
        plate.text = plateData;

        Debug.LogFormat("[Maintenance #{0}] The model of the car is {1}.", moduleId, carModel);
        Debug.LogFormat("[Maintenance #{0}] The car was manufactured in {1} {2}.", moduleId, plated, carYear);
        Debug.LogFormat("[Maintenance #{0}] The number plate of the car is {1}.", moduleId, plateData);

        //Calculate the car value & insurance company
        carValue = carBaseValue * carModelMultiplier;
        Debug.LogFormat("[Maintenance #{0}] The value of the car is £{1}.", moduleId, carValue);

        insuranceCollection();
        Debug.LogFormat("[Maintenance #{0}] The insurance company is {1}.", moduleId, insurance);

        //Calculate jobs and base price
        jobsGrabber();
        jobCalculator();
        Debug.LogFormat("[Maintenance #{0}] There are {1} jobs that need attention: {2}.", moduleId, numberOfJobs, string.Join(", ", jobsRequired.ToArray()));
        Debug.LogFormat("[Maintenance #{0}] The cost of the required jobs after any insurance discount will be £{1}.", moduleId, jobsCost);

        //Re-order the jobs lists
        correctJobsOrder = jobsRequired.OrderBy(job => jobsOrder.IndexOf(job)).ToList();

        //Determine "write-off" status
        if (carModel == "Honda" && (carYear == "2001" || carYear == "2002" || carYear == "2003"))
        {
            writeOff = "true";
            Debug.LogFormat("[Maintenance #{0}] We are sorry to inform you that your car is a write-off due to being a pre-2004 Honda.", moduleId);
        }
        else if (carValue < jobsCost)
        {
            writeOff = "true";
            Debug.LogFormat("[Maintenance #{0}] We are sorry to inform you that your car is a write-off due to the cost of repairs being £{1} greater than the value of the car.", moduleId, jobsCost - carValue);
        }
        else
        {
            Debug.LogFormat("[Maintenance #{0}] Good news, your car is NOT a write-off. The jobs MUST be completed in the following order: {1}.", moduleId, string.Join(", ", correctJobsOrder.ToArray()));
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Type '!{0} Brake fluid change, Headlight bulb, Wash' to perform repairs in that order. Abbreviations are allowed, but note that ambiugous ones like “Windscreen” may trigger either “Windscreen replacement” or “Windscreen chip”.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        foreach (var piece in command.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            for (int i = 0; i < jobEntries.Count; i++)
            {
                if (jobEntries[jobIndex].IndexOf(piece.Trim(), StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    repairBut.OnInteract();
                    yield return new WaitForSeconds(.5f);
                    goto repairDone;
                }

                jobRight.OnInteract();
                yield return new WaitForSeconds(.1f);
            }

            yield return string.Format("sendtochat I couldn’t find a repair job called “{0}”.", piece);
            yield break;

            repairDone:;
        }
    }

    //Buttons
    public void OnjobLeft()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, jobLeft.transform);
        jobLeft.AddInteractionPunch(.5f);
        jobIndex = ((jobIndex + jobEntries.Count) - 1) % jobEntries.Count;
        jobText.text = jobEntries[jobIndex];
    }
    public void OnjobRight()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, jobRight.transform);
        jobRight.AddInteractionPunch(.5f);
        jobIndex = (jobIndex + 1) % jobEntries.Count;
        jobText.text = jobEntries[jobIndex];
    }
    public void OnrepairBut()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, repairBut.transform);
        repairBut.AddInteractionPunch();

        switch (stage)
        {
            case 1:
                if (writeOff == "true" && jobText.text == "Write-off")
                {
                    GetComponent<KMBombModule>().HandlePass();
                    light1.material.mainTexture = onMat;
                    light2.material.mainTexture = onMat;
                    light3.material.mainTexture = onMat;
                    light4.material.mainTexture = onMat;
                    Audio.PlaySoundAtTransform("engineJudder", transform);
                    Debug.LogFormat("[Maintenance #{0}] Your car has been written-off. Module disarmed.", moduleId);
                    stage++;
                    stage++;
                    stage++;
                    stage++;
                }
                else if (jobText.text == correctJobsOrder[0] && writeOff != "true")
                {
                    light1.material.mainTexture = onMat;
                    Audio.PlaySoundAtTransform("engineJudder", transform);
                    Debug.LogFormat("[Maintenance #{0}] You have selected {1}. The necessary maintenance has been carried out.", moduleId, correctJobsOrder[0]);
                    stage++;
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Maintenance #{0}] Strike! You pressed {1}. That is incorrect.", moduleId, jobText.text);

                }
                break;

            case 2:
                if (numberOfJobs == 2 && jobText.text == correctJobsOrder[1] && writeOff != "true")
                {
                    light2.material.mainTexture = onMat;
                    Audio.PlaySoundAtTransform("engineStart", transform);
                    Debug.LogFormat("[Maintenance #{0}] Your have selected {1}. The necessary maintenance has been carried out. Module disarmed. Please drive safely.", moduleId, correctJobsOrder[1]);
                    GetComponent<KMBombModule>().HandlePass();
                    stage++;
                    stage++;
                    stage++;
                }
                else if (jobText.text == correctJobsOrder[1] && writeOff != "true")
                {
                    light2.material.mainTexture = onMat;
                    Audio.PlaySoundAtTransform("engineJudder", transform);
                    Debug.LogFormat("[Maintenance #{0}] Your have selected {1}. The necessary maintenance has been carried out.", moduleId, correctJobsOrder[1]);
                    stage++;
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Maintenance #{0}] Strike! You pressed {1}. That is incorrect.", moduleId, jobText.text);
                }
                break;

            case 3:
                if (numberOfJobs == 3 && jobText.text == correctJobsOrder[2] && writeOff != "true")
                {
                    light3.material.mainTexture = onMat;
                    Audio.PlaySoundAtTransform("engineStart", transform);
                    Debug.LogFormat("[Maintenance #{0}] Your have selected {1}. The necessary maintenance has been carried out. Module disarmed. Please drive safely.", moduleId, correctJobsOrder[2]);
                    GetComponent<KMBombModule>().HandlePass();
                    stage++;
                    stage++;
                }
                else if (jobText.text == correctJobsOrder[2] && writeOff != "true")
                {
                    light3.material.mainTexture = onMat;
                    Audio.PlaySoundAtTransform("engineJudder", transform);
                    Debug.LogFormat("[Maintenance #{0}] Your have selected {1}. The necessary maintenance has been carried out.", moduleId, correctJobsOrder[2]);
                    stage++;
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Maintenance #{0}] Strike! You pressed {1}. That is incorrect.", moduleId, jobText.text);
                }
                break;

            case 4:
                if (jobText.text == correctJobsOrder[3] && writeOff != "true")
                {
                    light4.material.mainTexture = onMat;
                    Audio.PlaySoundAtTransform("engineStart", transform);
                    Debug.LogFormat("[Maintenance #{0}] Your have selected {1}. The necessary maintenance has been carried out. Module disarmed. Please drive safely.", moduleId, correctJobsOrder[3]);
                    GetComponent<KMBombModule>().HandlePass();
                    stage++;
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Maintenance #{0}] Strike! You pressed {1}. That is incorrect.", moduleId, jobText.text);
                }
                break;

            default:
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Maintenance #{0}] Strike! This module has been disarmed. Please stop pressing buttons.", moduleId);
                break;
        }
    }




    //Job calculator
    void jobCalculator()
    {
        if (insurance.StartsWith("A") && plated == "September" && plateData.Contains("M") && (carModel == "Mercedes-Benz" || carModel == "Porsche" || carModel == "Ferrari"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("Four tyres");
                jobsCost = wash + fourTyres;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("Four tyres");
                jobsRequired.Add("Exhaust welding");
                jobsCost = wash + fourTyres + exhaustWelding;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("Four tyres");
                jobsRequired.Add("Exhaust welding");
                jobsRequired.Add("Head gasket replacement");
                jobsCost = wash + fourTyres + exhaustWelding + headGasketReplacement;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is K", moduleId);
        }

        else if (insurance.StartsWith("A") && plated == "September" && plateData.Contains("M"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("Headlight bulb");
                jobsCost = wash + headlightBulb;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Wiper replacement");
                jobsCost = wash + headlightBulb + wiperReplacement;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Wiper replacement");
                jobsRequired.Add("Oil change");
                jobsCost = wash + headlightBulb + wiperReplacement + oilChange;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is N", moduleId);
        }

        else if (plated == "September" && plateData.Contains("M") && (carModel == "Mercedes-Benz" || carModel == "Porsche" || carModel == "Ferrari"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Head gasket replacement");
                jobsRequired.Add("Brake fluid change");
                jobsCost = headGasketReplacement + brakeFluidChange;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Head gasket replacement");
                jobsRequired.Add("Brake fluid change");
                jobsRequired.Add("Wiper replacement");
                jobsCost = headGasketReplacement + brakeFluidChange + wiperReplacement;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Head gasket replacement");
                jobsRequired.Add("Brake fluid change");
                jobsRequired.Add("Wiper replacement");
                jobsRequired.Add("Windscreen replacement");
                jobsCost = headGasketReplacement + brakeFluidChange + wiperReplacement + windscreenReplacement;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is H", moduleId);
        }

        else if (insurance.StartsWith("A") && plated == "September" && (carModel == "Mercedes-Benz" || carModel == "Porsche" || carModel == "Ferrari"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Exhaust welding");
                jobsRequired.Add("Two tyres");
                jobsCost = exhaustWelding + twoTyres;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Exhaust welding");
                jobsRequired.Add("Two tyres");
                jobsRequired.Add("Headlight bulb");
                jobsCost = exhaustWelding + twoTyres + headlightBulb;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Exhaust welding");
                jobsRequired.Add("Two tyres");
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Brake fluid change");
                jobsCost = exhaustWelding + twoTyres + headlightBulb + brakeFluidChange;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is O", moduleId);
        }

        else if (insurance.StartsWith("A") && plateData.Contains("M") && (carModel == "Mercedes-Benz" || carModel == "Porsche" || carModel == "Ferrari"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("One tyre");
                jobsCost = wash + oneTyre;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Wiper replacement");
                jobsCost = wash + oneTyre + wiperReplacement;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Wiper replacement");
                jobsRequired.Add("Windscreen chip");
                jobsCost = wash + oneTyre + wiperReplacement + windscreenChip;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is G", moduleId);
        }

        else if (plateData.Contains("M") && plated == "September")
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Windscreen chip");
                jobsRequired.Add("Oil change");
                jobsCost = windscreenChip + oilChange;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Windscreen chip");
                jobsRequired.Add("Oil change");
                jobsRequired.Add("Four tyres");
                jobsCost = windscreenChip + oilChange + fourTyres;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Windscreen chip");
                jobsRequired.Add("Oil change");
                jobsRequired.Add("Four tyres");
                jobsRequired.Add("Headlight bulb");
                jobsCost = windscreenChip + oilChange + fourTyres + headlightBulb;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is E", moduleId);
        }

        else if (insurance.StartsWith("A") && plateData.Contains("M"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Wiper replacement");
                jobsCost = oneTyre + wiperReplacement;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Wiper replacement");
                jobsRequired.Add("Brake fluid change");
                jobsCost = oneTyre + wiperReplacement + brakeFluidChange;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Wiper replacement");
                jobsRequired.Add("Brake fluid change");
                jobsRequired.Add("Windscreen replacement");
                jobsCost = oneTyre + wiperReplacement + brakeFluidChange + windscreenReplacement;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is L", moduleId);
        }

        else if (insurance.StartsWith("A") && (carModel == "Mercedes-Benz" || carModel == "Porsche" || carModel == "Ferrari"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Oil change");
                jobsRequired.Add("Four tyres");
                jobsCost = oilChange + fourTyres;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Oil change");
                jobsRequired.Add("Four tyres");
                jobsRequired.Add("Windscreen chip");
                jobsCost = oilChange + fourTyres + windscreenChip;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Oil change");
                jobsRequired.Add("Four tyres");
                jobsRequired.Add("Windscreen chip");
                jobsRequired.Add("Wash");
                jobsCost = oilChange + fourTyres + windscreenChip + wash;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is D", moduleId);
        }

        else if (insurance.StartsWith("A") && plated == "September")
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Windscreen replacement");
                jobsRequired.Add("One tyre");
                jobsCost = windscreenReplacement + oneTyre;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Windscreen replacement");
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Brake fluid change");
                jobsCost = windscreenReplacement + oneTyre + brakeFluidChange;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Windscreen replacement");
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Brake fluid change");
                jobsRequired.Add("Wiper replacement");
                jobsCost = windscreenReplacement + oneTyre + brakeFluidChange + wiperReplacement;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is P", moduleId);
        }

        else if (plated == "September" && (carModel == "Mercedes-Benz" || carModel == "Porsche" || carModel == "Ferrari"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Two tyres");
                jobsCost = headlightBulb + twoTyres;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Two tyres");
                jobsRequired.Add("Exhaust welding");
                jobsCost = headlightBulb + twoTyres + exhaustWelding;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Two tyres");
                jobsRequired.Add("Exhaust welding");
                jobsRequired.Add("Wiper replacement");
                jobsCost = headlightBulb + twoTyres + exhaustWelding + wiperReplacement;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is M", moduleId);
        }

        else if (plateData.Contains("M") && (carModel == "Mercedes-Benz" || carModel == "Porsche" || carModel == "Ferrari"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Windscreen replacement");
                jobsRequired.Add("Oil change");
                jobsCost = windscreenReplacement + oilChange;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Windscreen replacement");
                jobsRequired.Add("Oil change");
                jobsRequired.Add("Brake fluid change");
                jobsCost = windscreenReplacement + oilChange + brakeFluidChange;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Windscreen replacement");
                jobsRequired.Add("Oil change");
                jobsRequired.Add("Brake fluid change");
                jobsRequired.Add("Head gasket replacement");
                jobsCost = windscreenReplacement + oilChange + brakeFluidChange + headGasketReplacement;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is F", moduleId);
        }

        else if (carModel == "Mercedes-Benz" || carModel == "Porsche" || carModel == "Ferrari")
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Head gasket replacement");
                jobsCost = headlightBulb + headGasketReplacement;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Head gasket replacement");
                jobsRequired.Add("Windscreen replacement");
                jobsCost = headlightBulb + headGasketReplacement + windscreenReplacement;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Head gasket replacement");
                jobsRequired.Add("Windscreen replacement");
                jobsRequired.Add("Four tyres");
                jobsCost = headlightBulb + headGasketReplacement + windscreenReplacement + fourTyres;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is B", moduleId);
        }

        else if (plated == "September")
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Exhaust welding");
                jobsCost = oneTyre + exhaustWelding;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Exhaust welding");
                jobsRequired.Add("Wash");
                jobsCost = oneTyre + exhaustWelding + wash;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Exhaust welding");
                jobsRequired.Add("Wash");
                jobsRequired.Add("Headlight bulb");
                jobsCost = oneTyre + exhaustWelding + wash + headlightBulb;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is J", moduleId);
        }

        else if (plateData.Contains("M"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Wiper replacement");
                jobsRequired.Add("Headlight bulb");
                jobsCost = wiperReplacement + headlightBulb;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Wiper replacement");
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Wash");
                jobsCost = wiperReplacement + headlightBulb + wash;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Wiper replacement");
                jobsRequired.Add("Headlight bulb");
                jobsRequired.Add("Wash");
                jobsRequired.Add("Windscreen chip");
                jobsCost = wiperReplacement + headlightBulb + wash + windscreenChip;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is C", moduleId);
        }

        else if (insurance.StartsWith("A"))
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("One tyre");
                jobsCost = wash + oneTyre;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Windscreen chip");
                jobsCost = wash + oneTyre + windscreenChip;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Wash");
                jobsRequired.Add("One tyre");
                jobsRequired.Add("Windscreen chip");
                jobsRequired.Add("Exhaust welding");
                jobsCost = wash + oneTyre + windscreenChip + exhaustWelding;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is I", moduleId);
        }

        else
        {
            if (numberOfJobs == 2)
            {
                jobsRequired.Add("Brake fluid change");
                jobsRequired.Add("Wash");
                jobsCost = brakeFluidChange + wash;
            }
            else if (numberOfJobs == 3)
            {
                jobsRequired.Add("Brake fluid change");
                jobsRequired.Add("Wash");
                jobsRequired.Add("Two tyres");
                jobsCost = brakeFluidChange + wash + twoTyres;
            }
            else if (numberOfJobs == 4)
            {
                jobsRequired.Add("Brake fluid change");
                jobsRequired.Add("Wash");
                jobsRequired.Add("Two tyres");
                jobsRequired.Add("Exhaust welding");
                jobsCost = brakeFluidChange + wash + twoTyres + exhaustWelding;
            }
            Debug.LogFormat("[Maintenance #{0}] The correct Venn diagram letter is A", moduleId);
        }

    }
    //Plate A Collection
    void plateACollection()
    {
        int plateA = UnityEngine.Random.Range(0, 9);

        switch (plateA)
        {
            case 0:
                plateAData = "FD";
                carModelMultiplier = 4;
                carModel = "Ford";
                break;
            case 1:
                plateAData = "RN";
                carModelMultiplier = 3;
                carModel = "Renault";
                break;
            case 2:
                plateAData = "PC";
                carModelMultiplier = 9;
                carModel = "Porsche";
                break;
            case 3:
                plateAData = "BN";
                carModelMultiplier = 8;
                carModel = "Mercedes-Benz";
                break;
            case 4:
                plateAData = "MA";
                carModelMultiplier = 5;
                carModel = "Mazda";
                break;
            case 5:
                plateAData = "HN";
                carModelMultiplier = 2;
                carModel = "Honda";
                break;
            case 6:
                plateAData = "AD";
                carModelMultiplier = 7;
                carModel = "Audi";
                break;
            case 7:
                plateAData = "BM";
                carModelMultiplier = 6;
                carModel = "BMW";
                break;
            case 8:
                plateAData = "FR";
                carModelMultiplier = 10;
                carModel = "Ferrari";
                break;
        }
    }
    //Plate B Collection
    void plateBCollection()
    {
        int plateB = UnityEngine.Random.Range(0, 37);

        switch (plateB)
        {
            case 0:
                plateBData = "51 ";
                carBaseValue = 50;
                carYear = "2001";
                plated = "September";
                break;
            case 1:
                plateBData = "02 ";
                carBaseValue = 60;
                carYear = "2002";
                plated = "March";
                break;
            case 2:
                plateBData = "52 ";
                carBaseValue = 60;
                carYear = "2002";
                plated = "September";
                break;
            case 3:
                plateBData = "03 ";
                carBaseValue = 70;
                carYear = "2003";
                plated = "March";
                break;
            case 4:
                plateBData = "53 ";
                carBaseValue = 70;
                carYear = "2003";
                plated = "September";
                break;
            case 5:
                plateBData = "04 ";
                carBaseValue = 80;
                carYear = "2004";
                plated = "March";
                break;
            case 6:
                plateBData = "54 ";
                carBaseValue = 80;
                carYear = "2004";
                plated = "September";
                break;
            case 7:
                plateBData = "05 ";
                carBaseValue = 90;
                carYear = "2005";
                plated = "March";
                break;
            case 8:
                plateBData = "55 ";
                carBaseValue = 90;
                carYear = "2005";
                plated = "September";
                break;
            case 9:
                plateBData = "06 ";
                carBaseValue = 100;
                carYear = "2006";
                plated = "March";
                break;
            case 10:
                plateBData = "56 ";
                carBaseValue = 100;
                carYear = "2006";
                plated = "September";
                break;
            case 11:
                plateBData = "07 ";
                carBaseValue = 125;
                carYear = "2007";
                plated = "March";
                break;
            case 12:
                plateBData = "57 ";
                carBaseValue = 125;
                carYear = "2007";
                plated = "September";
                break;
            case 13:
                plateBData = "08 ";
                carBaseValue = 150;
                carYear = "2008";
                plated = "March";
                break;
            case 14:
                plateBData = "58 ";
                carBaseValue = 150;
                carYear = "2008";
                plated = "September";
                break;
            case 15:
                plateBData = "09 ";
                carBaseValue = 175;
                carYear = "2009";
                plated = "March";
                break;
            case 16:
                plateBData = "59 ";
                carBaseValue = 175;
                carYear = "2009";
                plated = "September";
                break;
            case 17:
                plateBData = "10 ";
                carBaseValue = 200;
                carYear = "2010";
                plated = "March";
                break;
            case 18:
                plateBData = "60 ";
                carBaseValue = 200;
                carYear = "2010";
                plated = "September";
                break;
            case 19:
                plateBData = "11 ";
                carBaseValue = 250;
                carYear = "2011";
                plated = "March";
                break;
            case 20:
                plateBData = "61 ";
                carBaseValue = 250;
                carYear = "2011";
                plated = "September";
                break;
            case 21:
                plateBData = "12 ";
                carBaseValue = 300;
                carYear = "2012";
                plated = "March";
                break;
            case 22:
                plateBData = "62 ";
                carBaseValue = 300;
                carYear = "2012";
                plated = "September";
                break;
            case 23:
                plateBData = "13 ";
                carBaseValue = 400;
                carYear = "2013";
                plated = "March";
                break;
            case 24:
                plateBData = "63 ";
                carBaseValue = 400;
                carYear = "2013";
                plated = "September";
                break;
            case 25:
                plateBData = "14 ";
                carBaseValue = 500;
                carYear = "2014";
                plated = "March";
                break;
            case 26:
                plateBData = "64 ";
                carBaseValue = 500;
                carYear = "2014";
                plated = "September";
                break;
            case 27:
                plateBData = "15 ";
                carBaseValue = 600;
                carYear = "2015";
                plated = "March";
                break;
            case 28:
                plateBData = "65 ";
                carBaseValue = 600;
                carYear = "2015";
                plated = "September";
                break;
            case 29:
                plateBData = "16 ";
                carBaseValue = 700;
                carYear = "2016";
                plated = "March";
                break;
            case 30:
                plateBData = "66 ";
                carBaseValue = 700;
                carYear = "2016";
                plated = "September";
                break;
            case 31:
                plateBData = "17 ";
                carBaseValue = 800;
                carYear = "2017";
                plated = "March";
                break;
            case 32:
                plateBData = "67 ";
                carBaseValue = 800;
                carYear = "2017";
                plated = "September";
                break;
            case 33:
                plateBData = "18 ";
                carBaseValue = 900;
                carYear = "2018";
                plated = "March";
                break;
            case 34:
                plateBData = "68 ";
                carBaseValue = 900;
                carYear = "2018";
                plated = "September";
                break;
            case 35:
                plateBData = "19 ";
                carBaseValue = 1000;
                carYear = "2019";
                plated = "March";
                break;
            case 36:
                plateBData = "69 ";
                carBaseValue = 1000;
                carYear = "2019";
                plated = "September";
                break;
        }
    }
    //Plate C Collection
    void plateCCollection()
    {
        int plateC = UnityEngine.Random.Range(0, 22);

        switch (plateC)
        {
            case 0:
                plateCData += "A";
                break;
            case 1:
                plateCData += "B";
                break;
            case 2:
                plateCData += "C";
                break;
            case 3:
                plateCData += "D";
                break;
            case 4:
                plateCData += "E";
                break;
            case 5:
                plateCData += "F";
                break;
            case 6:
                plateCData += "G";
                break;
            case 7:
                plateCData += "H";
                break;
            case 8:
                plateCData += "J";
                break;
            case 9:
                plateCData += "K";
                break;
            case 10:
                plateCData += "L";
                break;
            case 11:
                plateCData += "M";
                break;
            case 12:
                plateCData += "N";
                break;
            case 13:
                plateCData += "P";
                break;
            case 14:
                plateCData += "R";
                break;
            case 15:
                plateCData += "S";
                break;
            case 16:
                plateCData += "T";
                break;
            case 17:
                plateCData += "U";
                break;
            case 18:
                plateCData += "V";
                break;
            case 19:
                plateCData += "W";
                break;
            case 20:
                plateCData += "X";
                break;
            case 21:
                plateCData += "Y";
                break;
        }
    }

    void insuranceCollection()
    {
        if (plateCData.EndsWith("A") || plateCData.EndsWith("B") || plateCData.EndsWith("C"))
        {
            insurance = "Admiral";
            brakeFluidChange = 0;
        }
        else if (plateCData.EndsWith("D") || plateCData.EndsWith("E") || plateCData.EndsWith("F"))
        {
            insurance = "Swift";
            oilChange = 0;
            oneTyre = 0;
        }
        else if (plateCData.EndsWith("G") || plateCData.EndsWith("H") || plateCData.EndsWith("J"))
        {
            insurance = "Axa";
            windscreenChip = 0;
        }
        else if (plateCData.EndsWith("K") || plateCData.EndsWith("L") || plateCData.EndsWith("M"))
        {
            insurance = "Swinton";
            windscreenChip = 0;
        }
        else if (plateCData.EndsWith("N") || plateCData.EndsWith("P") || plateCData.EndsWith("R"))
        {
            insurance = "Aviva";
            oneTyre = 0;
            twoTyres = 0;
        }
        else if (plateCData.EndsWith("S") || plateCData.EndsWith("T") || plateCData.EndsWith("U"))
        {
            insurance = "RAC";
            windscreenChip = 0;
            windscreenReplacement = 0;
        }
        else if (plateCData.EndsWith("V") || plateCData.EndsWith("W"))
        {
            insurance = "AA";
            oilChange = 0;
            windscreenChip = 0;
            brakeFluidChange = 0;
        }
        else if (plateCData.EndsWith("X") || plateCData.EndsWith("Y"))
        {
            insurance = "Hastings Direct";
            oilChange = 0;
        }
    }

    void jobsGrabber()
    {
        int jobNumber = UnityEngine.Random.Range(0, 3);

        switch (jobNumber)
        {
            case 0:
                numberOfJobs = 2;
                notebookText.text = date + "\n\nThere are 2\njobs that need\nattention";
                light1.enabled = true;
                light2.enabled = true;
                break;
            case 1:
                numberOfJobs = 3;
                notebookText.text = date + "\n\nThere are 3\njobs that need\nattention";
                light1.enabled = true;
                light2.enabled = true;
                light3.enabled = true;
                break;
            case 2:
                numberOfJobs = 4;
                notebookText.text = date + "\n\nThere are 4\njobs that need\nattention";
                light1.enabled = true;
                light2.enabled = true;
                light3.enabled = true;
                light4.enabled = true;
                break;
        }
    }
}
