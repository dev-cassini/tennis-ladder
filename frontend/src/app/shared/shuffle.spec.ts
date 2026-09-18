import { shuffled } from './shuffle';

describe('shuffled', () => {
  it('uses Fisher-Yates ordering without changing the source array', () => {
    const source = [1, 2, 3];

    const result = shuffled(source, () => 0);

    expect(result).toEqual([2, 3, 1]);
    expect(source).toEqual([1, 2, 3]);
  });

  it('preserves every item exactly once', () => {
    const result = shuffled(['A', 'B', 'C', 'D'], () => 0.5);

    expect([...result].sort()).toEqual(['A', 'B', 'C', 'D']);
  });
});
