import path from 'node:path';

export function stateDirectory(override) {
	const ewlFolder = process.env.EwlFolderPath ?? (process.platform === 'win32'
		? 'C:\\Enterprise Web Library' : '/opt/enterprise-web-library');
	const directory = override === undefined ? path.join(ewlFolder, 'Error Triage', 'HEY') : override;
	if (!directory?.trim() || !path.isAbsolute(directory))
		throw new Error('--state or EwlFolderPath must resolve to an absolute directory outside source control.');
	return directory;
}
