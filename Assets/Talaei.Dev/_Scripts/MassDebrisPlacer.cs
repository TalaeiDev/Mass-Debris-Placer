using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System.IO;

namespace Talaei.dev
{
    [ExecuteInEditMode]
    public class MassDebrisPlacer : MonoBehaviour
    {
        [Space(10), Header("Load Debris Asset")]
        public string debrisAssetPath = "Assets/Talaei.Dev/_Prefabs/Debris";
        [Space(5)]
        public bool loadDebrisAsset;
        [Space(5)]
        public List<GameObject> debris;

        [Space(10), Header("Debris Setting")]
        public int debrisCount = 20;
        public Vector2 randomScale = new Vector2(0.6f, 1.2f);
        [Range(0.1f, 50f)]
        public float radius = 1f;
        [Space(5)]
        public float simulationTime = 5f;
        [Space(10), Header("Debris Generate")]
        public bool generate = false;
        [Space(5)]
        public bool clear = false;
        [Space(5)]
        public bool fixPlace = false;

        [Space(10), Header("Mesh Setting")]
        public bool combineMesh = false;
        [Space(5)]
        public string meshSaveName = "DebrisMesh";
        public string savePath = "Talaei.Dev/_Mesh/Debris";
        [Space(5)]
        public bool generateLightmapUV = true;

        [Space(10), Header("Gizmo Setting")]
        public bool gizmo = true;
        public Color lineColor = new Color(1,0,0,1);
        public Color meshColor = new Color(0,0.6f,0.7f,0.14f);
        private Mesh mesh;

        [Space(30)]
        public List<Rigidbody> SceneRigidbody;

        private bool physicsSimulation;
        private bool saving;

        private float currentRadius;
        private float currentTime;

        private int i;
        private int collectorIndex;

        private GameObject DebrisRoot;
        private GameObject debriCombineRoot;

        private List<GameObject> debrisCollector;

        private Mesh lastMesh;

        private void OnEnable()
        {
            loadDebrisAsset = true;
            Texture2D icon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Talaei.Dev/_UI/Debris Icon.png", typeof(Texture2D));
            var editorGUIUtilityType = typeof(EditorGUIUtility);
            var bindingFlags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
            var args = new object[] { gameObject, icon };
            editorGUIUtilityType.InvokeMember("SetIconForObject", bindingFlags, null, null, args);

            physicsSimulation = false;
            currentRadius = radius;
            if (mesh == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(sphere);
            }
        }

        
        void OnDrawGizmosSelected()
        {
            if (gizmo == true)
            {
                Gizmos.color = lineColor;
                Gizmos.DrawWireSphere(transform.position, radius);
                Gizmos.color = meshColor;
                Gizmos.DrawMesh(mesh, transform.position, Quaternion.identity, new Vector3(radius * 2, radius * 2, radius * 2));
            }
            
        }

        public void ClearLog()
        {
            var assembly = Assembly.GetAssembly(typeof(Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }

        private void LoadDebrisAsset()
        {
            if(loadDebrisAsset == true)
            {
               if(debris != null)
                {
                    debris.Clear();
                }
                debris = new List<GameObject>();
                
                
                DirectoryInfo debrisPathInfo = new DirectoryInfo(debrisAssetPath);
                FileInfo[] debrisPrefabFile = debrisPathInfo.GetFiles("*.prefab");

                if(debrisPrefabFile.Length > 0)
                {
                    foreach (FileInfo fi in debrisPrefabFile)
                    {
                        string fullPath = fi.FullName.Replace(@"\", "/");
                        string debrisAssetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
                        GameObject prefab = AssetDatabase.LoadAssetAtPath(debrisAssetPath, typeof(GameObject)) as GameObject;
                        debris.Add(prefab);
                    }
                }
                
                loadDebrisAsset = false;
            }
        }

        void Update()
        {          
            if (currentRadius != radius)
            {
                currentRadius = radius;
            }

            DebrisGenerate();
            Clear();
            FixPlace();
            CombineMesh();
            PhysicsSimulation();
            LoadDebrisAsset();
        }



        void DebrisGenerate()
        {
            if (generate == true)
            {
                if(debris.Count > 0)
                {
                    if (debrisCollector == null)
                    {
                        debrisCollector = new List<GameObject>();
                    }

                    if (debrisCollector.Count > 0)
                    {
                        debrisCollector.Clear();
                    }

                    if (DebrisRoot != null)
                    {
                        DestroyImmediate(DebrisRoot);
                    }

                    foreach (Rigidbody rb in FindObjectsOfType<Rigidbody>())
                    {
                        if (rb.isKinematic == false)
                        {
                            SceneRigidbody.Add(rb);
                        }

                        rb.isKinematic = true;

                    }
                    currentTime = 0;
                    physicsSimulation = false;

                    DebrisRoot = new GameObject("Debris Root");
                    DebrisRoot.transform.position = transform.position;

                    for (i = 0; i < debrisCount; i++)
                    {

                        // pick a random debris object in debris array and Instantiate that in sceen with random postion,roation and scale 
                        int randomIndex = Random.Range(0, debris.Count - 1);
                        Vector3 randomPostion = Random.insideUnitSphere * radius + transform.position;
                        Vector3 randomRotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), 0);
                        float currentRandomScale = Random.Range(randomScale.x, randomScale.y);
                        var currentDebris = Instantiate(debris[randomIndex], new Vector3(randomPostion.x, transform.position.y, randomPostion.z), Quaternion.identity, DebrisRoot.transform);
                        currentDebris.transform.localEulerAngles = randomRotation;
                        currentDebris.transform.localScale = new Vector3(currentRandomScale, currentRandomScale, currentRandomScale);
                        currentDebris.AddComponent<MeshCollider>();
                        currentDebris.GetComponent<MeshCollider>().convex = true;
                        currentDebris.AddComponent<Rigidbody>();
                        currentDebris.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        debrisCollector.Add(currentDebris);
                        physicsSimulation = true;
                        currentTime = simulationTime;
                    }

                    if (i == debrisCount)
                    {
                        ClearLog();
                        generate = false;
                        i = 0;
                    }
                }

                else
                {
                    ClearLog();
                    Debug.Log("<color=#ff9900><b> \"" + "Debris list is empty add debris objects to list" + "\" </b></color>");
                    generate = false;
                }
                
            }

        }

        private void PhysicsSimulation()
        {
            // check if physcis simulation is true disable autoSimulation and simulate physics in editor or disable simulation in editor
            if (physicsSimulation == true)
            {
                currentTime -= Time.deltaTime;
                if (currentTime <= 0)
                {
                    Physics.autoSimulation = true;
                    foreach (Rigidbody rb in SceneRigidbody)
                    {
                        rb.isKinematic = false;
                    }
                    SceneRigidbody.Clear();
                    ClearLog();
                    Debug.Log("<color=#ff9900><b> \"" + "Simulating Finish" + "\" </b></color>");
                    physicsSimulation = false;
                }

                else
                {
                    Debug.Log("<color=#ff9900><b> \"" +"Simulating "+ currentTime.ToString() + "\" </b></color>");
                    if (Physics.autoSimulation == true)
                    {
                        Physics.autoSimulation = false;
                    }

                    Physics.Simulate(Time.fixedDeltaTime);
                }            
            }

            else
            {
                if (Physics.autoSimulation == false)
                {
                    Physics.autoSimulation = true;
                }
            }
        }

        void Clear()
        {
            // check if debris object exist in scene and not combined yet destroy all and clear debrisCollector array list
            if (clear == true )
            {
                if(debris.Count > 0)
                {
                    currentTime = 0;
                    physicsSimulation = false;
                    foreach (Rigidbody rb in SceneRigidbody)
                    {
                        rb.isKinematic = false;
                    }
                    SceneRigidbody.Clear();
                    if (debrisCollector.Count > 0 && debrisCollector != null)
                    {
                        debrisCollector.Clear();
                    }

                    if (DebrisRoot != null)
                    {
                        DestroyImmediate(DebrisRoot);
                    }
                    ClearLog();
                    Debug.Log("<color=#ff9900><b> \"" + "All debris has been deleted" + "\" </b></color>");
                }

                else
                {
                    ClearLog();
                    Debug.Log("<color=#ff9900><b> \"" + "There is no debris to be deleted" + "\" </b></color>");
                }
                clear = false;
            }          
        }

        void FixPlace()
        {
            // check if debrisCollector list not empty for each object on that destroy rigged body and fix object place on scene
            if (fixPlace == true)
            {
                currentTime = 0;
                physicsSimulation = false;
                foreach (Rigidbody rb in SceneRigidbody)
                {
                    rb.isKinematic = false;
                }
                SceneRigidbody.Clear();

                if (debrisCollector != null && debrisCollector.Count > 0)
                {
                    foreach (GameObject g in debrisCollector)
                    {
                        collectorIndex = debrisCollector.IndexOf(g);
                        DestroyImmediate(g.GetComponent<Rigidbody>());
                    }

                    if (collectorIndex == debrisCollector.Count - 1)
                    {
                        debrisCollector.Clear();
                        fixPlace = false;
                    }
                    ClearLog();
                    Debug.Log("<color=#ff9900><b> \"" + "All rigidbody componenet has been destroy on debris objects" + "\" </b></color>");
                }

                else
                {
                    fixPlace = false;
                    ClearLog();
                    Debug.Log("<color=#ff9900><b> \"" + "Mesh not found" + "\" </b></color>");
                }


            }
        }

        void CombineMesh()
        {
            if (combineMesh == true)
            {      
                if(DebrisRoot != null)
                {
                    var lastPostion = DebrisRoot.transform.position;
                    ArrayList materials = new ArrayList();
                    ArrayList combineInstanceArrays = new ArrayList();

                    // get all mesh in debris root and store all in a array
                    MeshFilter[] meshFilters = DebrisRoot.GetComponentsInChildren<MeshFilter>();

                    foreach (MeshFilter mf in meshFilters)
                    {
                        MeshRenderer meshRenderer = mf.GetComponent<MeshRenderer>();

                        if (!meshRenderer || !mf.sharedMesh || meshRenderer.sharedMaterials.Length != mf.sharedMesh.subMeshCount)
                        {
                            continue;
                        }

                        for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                        {
                            int materialArrayIndex = Contains(materials, meshRenderer.sharedMaterials[s].name);

                            if (materialArrayIndex == -1)
                            {
                                materials.Add(meshRenderer.sharedMaterials[s]);
                                materialArrayIndex = materials.Count - 1;
                            }
                            combineInstanceArrays.Add(new ArrayList());

                            CombineInstance combineInstance = new CombineInstance();
                            combineInstance.transform = meshRenderer.transform.localToWorldMatrix;
                            combineInstance.subMeshIndex = s;
                            combineInstance.mesh = mf.sharedMesh;
                            (combineInstanceArrays[materialArrayIndex] as ArrayList).Add(combineInstance);
                        }
                    }

                    // Create mesh filter & renderer
                    MeshFilter meshFilterCombine = DebrisRoot.GetComponent<MeshFilter>();
                    if (meshFilterCombine == null)
                    {
                        meshFilterCombine = DebrisRoot.AddComponent<MeshFilter>();
                    }
                    MeshRenderer meshRendererCombine = DebrisRoot.GetComponent<MeshRenderer>();
                    if (meshRendererCombine == null)
                    {
                        meshRendererCombine = DebrisRoot.AddComponent<MeshRenderer>();
                    }

                    // Combine by material index into per-material meshes
                    // also, Create CombineInstance array for next step
                    Mesh[] meshes = new Mesh[materials.Count];
                    CombineInstance[] combineInstances = new CombineInstance[materials.Count];

                    for (int m = 0; m < materials.Count; m++)
                    {
                        CombineInstance[] combineInstanceArray = (combineInstanceArrays[m] as ArrayList).ToArray(typeof(CombineInstance)) as CombineInstance[];
                        meshes[m] = new Mesh();
                        meshes[m].CombineMeshes(combineInstanceArray, true, true);

                        combineInstances[m] = new CombineInstance();
                        combineInstances[m].mesh = meshes[m];
                        combineInstances[m].subMeshIndex = 0;
                    }

                    // Combine into one
                    meshFilterCombine.sharedMesh = new Mesh();
                    meshFilterCombine.sharedMesh.CombineMeshes(combineInstances, false, false);

                    // Destroy other meshes
                    foreach (GameObject g in debrisCollector)
                    {
                        DestroyImmediate(g);
                    }

                    debrisCollector.Clear();

                    // Assign materials
                    Material[] materialsArray = materials.ToArray(typeof(Material)) as Material[];
                    meshRendererCombine.materials = materialsArray;

                    DebrisRoot.transform.localPosition = Vector3.zero;
                    debriCombineRoot = new GameObject("Debris Combined Root");
                    debriCombineRoot.transform.position = transform.position;
                    DebrisRoot.transform.parent = debriCombineRoot.transform;
                    lastMesh = DebrisRoot.GetComponent<MeshFilter>().sharedMesh;

                    if (lastMesh != null)
                    {
                        saving = true;
                        ClearLog();
                        Debug.Log("<color=#ff9900><b> \"" + "Start Saving Mesh" + "\" </b></color>");
                        SaveMesh(lastMesh, savePath);
                        DebrisRoot = null;
                    }
                  
                }

                else
                {
                    ClearLog();
                    Debug.Log("<color=#ff9900><b> \"" + "There is no mesh for combining" + "\" </b></color>");
                }
                combineMesh = false;

            }
                   
                       
        }
        private int Contains(ArrayList searchList, string searchName)
        {
            for (int i = 0; i < searchList.Count; i++)
            {
                if (((Material)searchList[i]).name == searchName)
                {
                    return i;
                }
            }
            return -1;
        }

        private void SaveMesh(Mesh mesh, string folderPath)
        {

            if (saving == true)
            {
                if (generateLightmapUV)
                {
                    UnwrapParam unwrapParam = new UnwrapParam();
                    UnwrapParam.SetDefaults(out unwrapParam);
                    Unwrapping.GenerateSecondaryUVSet(mesh, unwrapParam);
                }

                bool meshIsSaved = AssetDatabase.Contains(mesh);
               // Debug.Log(meshIsSaved);
             //   return;
                if (!meshIsSaved && !AssetDatabase.IsValidFolder("Assets/" + folderPath))
                {
                    string[] folderNames = folderPath.Split('/');
                    folderNames = folderNames.Where((folderName) => !folderName.Equals("")).ToArray();
                    folderNames = folderNames.Where((folderName) => !folderName.Equals(" ")).ToArray();

                    folderPath = "/"; // Reset folder path.
                    for (int i = 0; i < folderNames.Length; i++)
                    {
                        folderNames[i] = folderNames[i].Trim();
                        if (!AssetDatabase.IsValidFolder("Assets" + folderPath + folderNames[i]))
                        {
                            string folderPathWithoutSlash = folderPath.Substring(0, folderPath.Length - 1); // Delete last "/" character.
                            AssetDatabase.CreateFolder("Assets" + folderPathWithoutSlash, folderNames[i]);
                        }
                        folderPath += folderNames[i] + "/";
                    }
                    folderPath = folderPath.Substring(1, folderPath.Length - 2); // Delete first and last "/" character.
                }
                

                if (!meshIsSaved)
                {
                    string meshPath = "Assets/" + folderPath + "/" + meshSaveName + ".asset";
                    int assetNumber = 1;
                    Debug.Log(meshPath);
                    while (AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) != null) // If Mesh with same name exists, change name.
                    {
                        meshPath = "Assets/" + folderPath + "/" + meshSaveName +"_"+ assetNumber + ".asset";
                        assetNumber++;
                    }

                    AssetDatabase.CreateAsset(mesh, meshPath);
                    AssetDatabase.SaveAssets();
                    ClearLog();
                    Debug.Log("<color=#ff9900><b>Mesh \"" + mesh.name + "\" was saved in the \"" + folderPath + "\" folder.</b></color>"); // Show info about saved mesh.

                }
                saving = false;
            }

        }

    }
}

