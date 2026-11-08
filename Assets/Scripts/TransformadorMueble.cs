using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

// corre despues del ARTransformer: snap del giro, apilar y altura
public class TransformadorMueble : XRBaseGrabTransformer
{
    public float pasoGiro = 15f;

    [HideInInspector] public Pose ultimoObjetivo;
    [HideInInspector] public bool tieneObjetivo;

    Mueble mueble;
    float giroLibre;
    float ultimoSnap;
    float ultimoYaw;

    public override void OnLink(XRGrabInteractable grabInteractable)
    {
        base.OnLink(grabInteractable);
        mueble = grabInteractable.GetComponent<Mueble>();
    }

    public override void OnGrab(XRGrabInteractable grabInteractable)
    {
        base.OnGrab(grabInteractable);
        giroLibre = grabInteractable.transform.eulerAngles.y;
        ultimoSnap = Mathf.Round(giroLibre / pasoGiro) * pasoGiro;
        tieneObjetivo = false;
    }

    public override void OnGrabCountChanged(XRGrabInteractable grabInteractable, Pose targetPose, Vector3 localScale)
    {
        base.OnGrabCountChanged(grabInteractable, targetPose, localScale);
        if (grabInteractable.interactorsSelecting.Count > 0)
            ultimoYaw = grabInteractable.interactorsSelecting[0].GetAttachTransform(grabInteractable).localEulerAngles.y;
    }

    public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
    {
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic &&
            updatePhase != XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
            return;
        if (!grabInteractable.isSelected || mueble == null) return;

        // giro de los dos dedos, lo acumulo y redondeo cada 15 grados
        var interactor = grabInteractable.interactorsSelecting[0];
        float yaw = interactor.GetAttachTransform(grabInteractable).localEulerAngles.y;
        giroLibre += Mathf.DeltaAngle(ultimoYaw, yaw);
        ultimoYaw = yaw;
        float snap = Mathf.Round(giroLibre / pasoGiro) * pasoGiro;
        targetPose.rotation = Quaternion.Euler(0f, snap, 0f);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && !Mathf.Approximately(snap, ultimoSnap))
        {
            ultimoSnap = snap;
            Feedback.Tick();
        }

        // se apoya en el plano o arriba de otro mueble
        var p = targetPose.position;
        float apoyo = Apoyo.Calcular(p, mueble);
        targetPose.position = new Vector3(p.x, apoyo + mueble.alturaExtra, p.z);
        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic) mueble.apoyoY = apoyo;

        ultimoObjetivo = targetPose;
        tieneObjetivo = true;
    }
}
