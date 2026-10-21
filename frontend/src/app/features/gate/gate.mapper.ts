import { RuleCheckDto } from '../../core/api/gate.dto';
export function mapGateChecks(checks: readonly RuleCheckDto[]) {
  return checks.map((check) => ({
    ...check,
    state: check.state,
    labelState:
      check.state === 'Pass' ? 'Đạt' : check.state === 'Warning' ? 'Cảnh báo' : 'Không đạt',
  }));
}
