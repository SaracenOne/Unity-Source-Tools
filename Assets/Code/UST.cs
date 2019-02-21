#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace uSrcTools {
    public class UST : MonoBehaviour {

        static void LoadMap()
        {
            GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject currentGameObject in rootGameObjects)
            {
                if (currentGameObject != null)
                {
                    Test testComponent = currentGameObject.GetComponent<Test>();
                    uSrcSettings settingsComponent = currentGameObject.GetComponent<uSrcSettings>();
                    ResourceManager resourceManagerComponent = currentGameObject.GetComponent<ResourceManager>();
                    if (testComponent && settingsComponent && resourceManagerComponent)
                    {
                        settingsComponent.Setup();
                        resourceManagerComponent.Setup();
                        testComponent.Setup();

                        bool sceneModified = testComponent.loadBSP();
                        if (sceneModified)
                        {
                            //AssetDatabase.SaveAssets();
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        }
                    }
                }
            }

        }

        [MenuItem("Unity Source Tools/Generate Scene")]
        static void GenerateScene()
        {
            LoadMap();
        }

        void Start () {
		
	    }
    }
}
#endif