export interface ParsedStoryKey {
    prefix: string;
    number: number;
}

const STORY_KEY_REGEX = /^([A-Z][A-Z0-9]*)-(\d+)$/;

export function parseStoryKey(key: string): ParsedStoryKey | null {
    const match = key.trim().toUpperCase().match(STORY_KEY_REGEX);
    if (!match) return null;
    return {
        prefix: match[1],
        number: parseInt(match[2], 10),
    };
}

export function isValidStoryKey(key: string): boolean {
    return STORY_KEY_REGEX.test(key.trim().toUpperCase());
}

export function formatStoryKey(prefix: string, number: number): string {
    return `${prefix}-${number}`;
}
