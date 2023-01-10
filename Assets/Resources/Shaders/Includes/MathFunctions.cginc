float IsNaN(float x) {
    return (x < 0. || x > 0. || x == 0.) ? 0. : 1.;
}

float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0, 1);
    return lerp(b, a, h) - k * h * (1 - h);
}

float smax(float a, float b, float k) {
    return smin(a, b, -k);
}
