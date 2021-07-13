## Math

Explosion damage in this game is inversely proportional to the distance from the center, following the piecewise function
```
damage = d                if r <= R / 10
         (d R) / (10 r)   if r > R / 10
```

Where `d` is the a common factor for damage, `r` is the distance from the point of damage to the center of the explosion, and `R` is the maximum radius of the explosion.

On a side note, I think IRL explosion damage is roughly proportional to the inverse cube of distance, but for gameplay feel reasons, I'm following inverse proportional.

Integrating the equation above over the entire explosion radius, we can see that the total possible damage output of an explosion is

`19 pi R^2 d / 100`

## Code

The `d` factor should be calculated from known quantities, total damage and desired radius.

`ExplosionUtils.CalculateDamageFactor` is a function that takes in the rectangle and the center of the explosion and outputs the damage multiplier, using the formula described above.

Internally, `CalculateDamageFactor` uses a grid-based system to closely approximate the factor. It's analogous to a Riemann sum. It's not perfect, but it doesn't require me to figure out how to teach the computer to do integrals.
