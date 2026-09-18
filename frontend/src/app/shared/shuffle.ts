function secureRandom(): number {
  const value = new Uint32Array(1);
  crypto.getRandomValues(value);
  return value[0] / 2 ** 32;
}

export function shuffled<T>(items: readonly T[], random: () => number = secureRandom): T[] {
  const result = [...items];

  for (let index = result.length - 1; index > 0; index--) {
    const targetIndex = Math.floor(random() * (index + 1));
    [result[index], result[targetIndex]] = [result[targetIndex], result[index]];
  }

  return result;
}
