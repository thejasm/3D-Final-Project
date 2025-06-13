using MoreMountains.Feedbacks;
using UnityEngine;

public class ImpactShake: MonoBehaviour {
    public MMFeedbacks feedback;
    public AnimationCurve shakeFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0); // 0 = close, 1 = far
    public float maxDistance = 20f;
    private GameObject player;

    private void Start() {
        if (feedback == null) {
            feedback = GetComponent<MMFeedbacks>();
        }

        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null || feedback == null) {
            return;
        }

        float distance = Vector3.Distance(player.transform.position, this.transform.position);
        float t = Mathf.Clamp01(distance / maxDistance); // Normalize distance to [0,1]
        float falloff = shakeFalloffCurve.Evaluate(t);

        feedback.FeedbacksIntensity *= falloff;

        Debug.Log($"ImpactShake: Player distance {distance}, Curve t {t}, Falloff {falloff}, Intensity {feedback.FeedbacksIntensity}");

        feedback.PlayFeedbacks();
    }
}