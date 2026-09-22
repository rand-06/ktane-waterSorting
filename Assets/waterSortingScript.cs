using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepCoding;
using System.Text.RegularExpressions;

public class waterSortingScript : MonoBehaviour
{
	public Transform tubeContainer;
	public GameObject tubeObject;
	public AudioClip pourSound;
	

	void Awake()
	{
		initSettings();
		initTubes();
		initTubesConfiguration();
		//initColors();
		colorTubes();
	}

	//List<Color> colorsList = new List<Color>();
	List<Color> colorsList = new List<Color>
	{
		Color.red,
		Color.green,
		Color.blue,
		Color.yellow,
		Color.magenta,
		Color.cyan,
		new Color(1f,.5f,0),
		new Color(0.6f,1f,0f),
		new Color(0,1f,.5f),
		new Color(0,0.5f,1f),
		new Color(0.5f,0f,1f),
		new Color(1f,0f,.5f),
		Color.white,
		Color.black,
		new Color(0.5f,.25f,0)
	};
	//void initColors(){ for (int i=0; i<colors; i++) colorsList.Add(Color.HSVToRGB((float)i/colors,1,1)); }

	void colorTubes(){
		for (int i = 0; i<config.Count; i++){
			for (int j=0; j<config[i].Count; j++){
				water[i][j].GetComponent<MeshRenderer>().material.color = colorsList[config[i][j]-1];
			}
			for (int j = config[i].Count; j < sectors; j++)
			{
				water[i][j].GetComponent<MeshRenderer>().material.color = Color.gray;
			}
		}
	}

	private List<GameObject> tubes;
	private List<List<GameObject>> water;


	private List<List<int>> config = new List<List<int>>();

	bool checkForSolve() => config.Where(x=>x.Count>0).All(x => x.All(y => y == x[0]) && x.Count == sectors);
	
	bool doneShuffling() => config.Where(x=>x.Count>1).All(x=>Enumerable.Range(0,x.Count-1).All(i => x[i]!=x[i+1]));

	bool canPour(int start, int end, bool scrambleRuleset = false)=> !
		(config[end].Count == sectors || config[start].Count == 0 ||
			(config[end].Count > 0 && ((config[start].Last() == config[end].Last()) && scrambleRuleset)) || start == end);

	void pour(int start, int end, bool scrambleRuleset = false){
		if (!canPour(start, end, scrambleRuleset)) return;
		//print("Can pour.");
		if (scrambleRuleset){
			config[end].Add(config[start].Last());
			config[start].RemoveAt(config[start].Count - 1);
		}
		else{
			//print($"Start count: {config[start].Count}, end count: {config[end].Count}, last elements are: {config[start].LastOrDefault()} and {config[end].LastOrDefault()}");
			//while (config[start].Count > 0 && config[end].Count < sectors && (config[start].LastOrDefault() == config[end].LastOrDefault() || config[end].LastOrDefault() == 0)){
			int pourAmount = 1;
			if (config[start].Count > 1)
			{
				pourAmount = config[start].Select(x => x == config[start].Last()).Reverse().TakeWhile(x=>x).Count();
				if (pourAmount > sectors - config[end].Count) pourAmount = sectors - config[end].Count;
			}
			for (int i=0; i<pourAmount; i++){
				config[end].Add(config[start].Last());
				config[start].RemoveAt(config[start].Count - 1);
			}
			GetComponent<KMAudio>().PlaySoundAtTransform(pourSound.name, transform);
		}
	}
/*
	bool forcePour(int start, int end){
		if (config[end].Count == sectors || config[start].Count == 0) return false;
		config[end].Add(config[start].Last());
		config[start].RemoveAt(config[start].Count - 1);
		return true;
	}
*/
	void initTubesConfiguration(){
		// GENERATING SOLVED STATE
		for (int i=0; i<tubesAmount; i++){
			config.Add(new List<int>());
			for (int j=0; j<sectors; j++){
				config[i].Add(i%colors + 1);
			}
		}
		for (int i=0; i<emptiesAmount; i++) config.Add(new List<int>());

		//SHUFFLING (hopefully)
		//for (int i=0; i<10*tubesAmount*sectors; i++){
		while (!doneShuffling()){
			int start = Enumerable.Range(0, config.Count).Where(x => Enumerable.Range(0, config.Count).Any(y => canPour(x,y,true))).OrderBy(_ => UnityEngine.Random.value).FirstOrDefault(-1);
			if (start == -1) return; //just woteva, sectors/tubes is way greater than colors
			int end = Enumerable.Range(0, config.Count).Where(y => canPour(start,y,true)).PickRandom();
			pour(start,end,true);
		}
	}
	
	void initTubes()
	{
		tubes = Enumerable.Range(0,tubesAmount+emptiesAmount)
			.Select(_ => Instantiate(tubeObject, tubeContainer)).ToList();
		tubeObject.SetActive(false);
		GetComponent<KMSelectable>().Children = tubes.Select(x => x.GetComponent<KMSelectable>()).ToArray();
		GetComponent<KMSelectable>().UpdateChildrenProperly();
		water = Enumerable.Range(0,tubes.Count).Select(_ => new List<GameObject>()).ToList();
		for (int i = 0; i < tubes.Count; i++)
			water[i] = Enumerable.Range(0, sectors)
					.Select(_ => Instantiate(
						tubes[i].GetComponentInChildren<waterComponent>().gameObject,
						tubes[i].transform)).ToList();

		for (int i = 0; i < water.Count; i++)
		{
			tubes[i].GetComponentInChildren<waterComponent>().gameObject.SetActive(false);
			for (int j = 0; j < water[i].Count; j++)
			{
				water[i][j].transform.localScale = new Vector3(1, .5f, 1f / sectors)*0.8f;
				water[i][j].transform.localPosition = new Vector3(0,.6f,-.4f+.8f/(2*sectors)+j*.8f/sectors);
				//-0.4+.8/(2*sectors); inc = .8/sectors
			}
		}

		Vector3 size = tubeScale(tubes.Count);
		List<Vector3> positions = tubePosition(tubes.Count);
		
		for (int i = 0; i < tubes.Count; i++)
		{
			tubes[i].transform.localScale = size;	
			tubes[i].transform.localPosition = positions[i];
		}

		for (int i = 0; i < tubes.Count; i++){
			int i1 = i;
			tubes[i1].GetComponent<KMSelectable>().OnInteract += delegate
			{
				handlePress(i1);
				return false;
			};
		}
	}

	int currentSelectedTube = -1;
	void handlePress(int index){
		if (currentSelectedTube == -1){
			currentSelectedTube = index;
			tubes[currentSelectedTube].GetComponent<MeshRenderer>().material.color = Color.white;
		}
		else{
			pour(currentSelectedTube, index);
			tubes[currentSelectedTube].GetComponent<MeshRenderer>().material.color =
				new Color(0xc3 / 256f, 0xc3 / 256f, 0xc3 / 256f);
			currentSelectedTube = -1;
			colorTubes();
			
			if (checkForSolve()) GetComponent<KMBombModule>().HandlePass();
		}
	}

	Vector3 tubeScale(int total)
	{
		Vector3 init = new Vector3(.03f, .001f, .1f);
		int rows = Mathf.CeilToInt(Mathf.Sqrt(total / 3f));
		return init / rows;
	}
	
	List<Vector3> tubePosition(int N)
	{
		List<int> Q = Enumerable.Range(0, N).ToList();
		int R = Mathf.CeilToInt(Mathf.Sqrt(N / 3f));
		List<int> F = Q.Select(x => (R*x+R-1)/N).ToList();
		List<int> W1 = Enumerable.Range(0, R).Select(x => N*x/R).ToList();
		List<int> W = Enumerable.Range(1, R).Select(x => N*x/R - W1[x-1]).ToList();
		int M = 3 * R;
		List<float> X = Enumerable.Range(0, R).Select(x => -.5f*(W[x] - 1)/(M-1)).ToList();
		float A = .75f;
		List<int> P = Q.Select(x => x-W1[F[x]]).ToList();
		List<Vector3> result = Q.Select(x => new Vector3(
			X[F[x]]+(float)P[x]/(M-1), 0f, A*(R-1)/R - (5-R)/4f*F[x]
		)*.1f).ToList();
		return result;
	}
	
	private const int MAX_TUBES = 15;
	private const int MAX_SECTORS = 10;
	private int tubesAmount;
	private int emptiesAmount;
	private int sectors;
	private int colors;

	private WaterSortingSettings Settings = new WaterSortingSettings();

	void initSettings()
	{
		ModConfig<WaterSortingSettings> modConfig = new ModConfig<WaterSortingSettings>("WaterSortingSettings");
		Settings = modConfig.Settings;
		modConfig.Settings = Settings;
		TryOverrideMission();
		tubesAmount = Settings.tubesAmount < 2 || Settings.tubesAmount>MAX_TUBES?5:Settings.tubesAmount;
		emptiesAmount = Settings.emptiesAmount < 2 || Settings.emptiesAmount>tubesAmount?2:Settings.emptiesAmount;
		sectors = Settings.sectors < 2 || Settings.sectors>MAX_SECTORS?4:Settings.sectors;
		colors = Settings.colors < 2 || Settings.colors>tubesAmount?tubesAmount:Settings.colors;
	}
	
	void TryOverrideMission()
	{
		var desc = Game.Mission.Description ?? "";
		Match regexMatchCountVariants = Regex.Match(desc, @"\[Water Sorting\]\s(\d+),(\d+),(\d+),(\d+)");
		if (!regexMatchCountVariants.Success) return;
		Settings.tubesAmount = regexMatchCountVariants.Groups[1].Value.TryParseInt() ?? 5;
		Settings.emptiesAmount = regexMatchCountVariants.Groups[2].Value.TryParseInt() ?? 2;
		Settings.sectors = regexMatchCountVariants.Groups[3].Value.TryParseInt() ?? 4;
		Settings.colors = regexMatchCountVariants.Groups[4].Value.TryParseInt() ?? 5;
	}
	
	class WaterSortingSettings
	{
		public int tubesAmount = 5;
		public int emptiesAmount = 2;
		public int sectors = 4;
		public int colors = 5;
	}

	static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
	{
		new Dictionary<string, object>
		{
			{ "Filename", "WaterSortingSettings.json" },
			{ "Name", "Water Sorting Settings" },
			{ "Listings", new List<Dictionary<string, object>>{
				new Dictionary<string, object>
				{
					{ "Key", "tubesAmount" },
					{ "Text", "Tubes" },
					{ "Description", "Amount of filled tubes. Default is 5." }
				},
				new Dictionary<string, object>
				{
					{ "Key", "emptiesAmount" },
					{ "Text", "Empties" },
					{ "Description", "Amount of empty tubes. Default is 2." }
				},
				new Dictionary<string, object>
				{
					{ "Key", "sectors" },
					{ "Text", "Sectors" },
					{ "Description", "Amount of sectors within one tube. Default is 4." }
				}
				,
				new Dictionary<string, object>
				{
					{ "Key", "colors" },
					{ "Text", "Colors" },
					{ "Description", "Amount of colors. Cannot exceed amount of filled tubes. Default is 5." }
				}
			} }
		}
	};
	
	private string TwitchHelpMessage = "Use !{0}"
	
}
