using com.xucian.upm.grabtex;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class test : MonoBehaviour
{
    public string url;

    // Start is called before the first frame update
    void Start()
    {

        var rawImage = gameObject.GetComponent<RawImage>();
        _ = new GrabTex().IntoAsync(url, rawImage, CancellationToken.None);



    }

	// Update is called once per frame
	void Update()
    {
        
    }
}
