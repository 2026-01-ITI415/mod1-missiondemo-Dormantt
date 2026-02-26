using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slingshot : MonoBehaviour{

    [Header("Inscribed")]
    public GameObject projectilePrefab;
    public float velocityMult = 10f;
    public GameObject projLinePrefab;

    [Header("Dynamic")]
    public GameObject launchPoint;
    public Vector3 launchPos;
    public GameObject projectile;
    public bool aimingMode;

    void Awake(){
        Transform launchPointTrans = transform.Find("LaunchPoint");
        launchPoint = launchPointTrans.gameObject;
        launchPoint.SetActive(false);
        launchPos = launchPointTrans.position;
    }
 
 void OnMouseEnter(){
    //print("Slingshot:OnMouseEnter()");
    launchPoint.SetActive(true);
 }

 void OnMouseExit(){
    //print("Slingshot:OnMouseExit()");
    launchPoint.SetActive(false);
 }

 void OnMouseDown(){
    if (MissionDemolition.S != null && !MissionDemolition.S.SlingshotIsEnabled) return;

    aimingMode = true;

    projectile = Instantiate(projectilePrefab) as GameObject;

    projectile.transform.position = launchPos;

    projectile.GetComponent<Rigidbody>().isKinematic = true;
 }
 void Update(){

    if (!aimingMode) return;
    if (projectile == null){
        aimingMode = false;
        return;
    }

    Vector3 mousePos2D = Input.mousePosition;
    mousePos2D.z = -Camera.main.transform.position.z;
    Vector3 mousePos3D = Camera.main.ScreenToWorldPoint(mousePos2D);

    Vector3 mouseDelta = mousePos3D -launchPos;

    float maxMagnitude = this.GetComponent<SphereCollider>().radius;
    if (mouseDelta.magnitude > maxMagnitude){
        mouseDelta.Normalize();
        mouseDelta*= maxMagnitude;
    }

    Vector3 projPos = launchPos + mouseDelta;
    projectile.transform.position = projPos;

    if(Input.GetMouseButtonUp(0)){

        aimingMode = false;
        Rigidbody projRB = projectile.GetComponent<Rigidbody>();
        projRB.isKinematic = false;
        projRB.collisionDetectionMode = CollisionDetectionMode.Continuous;
        projRB.linearVelocity = -mouseDelta * velocityMult;
        if (MissionDemolition.S != null){
            MissionDemolition.S.RegisterShot();
        }
        FollowCam.POI = projectile;
        Instantiate<GameObject>(projLinePrefab, projectile.transform);
        projectile = null;
    }
 }

 public void CancelAiming(){
    aimingMode = false;
    if (projectile != null){
        Destroy(projectile);
        projectile = null;
    }
 }
}
