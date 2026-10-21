import { AssignContainersResult } from '../../core/api/orders.dto';
export function assignmentMessage(result: AssignContainersResult): string {
  return `Đã gán ${result.assignedCount}/${result.requestedCount} container.` +
    (result.failedContainerNumbers.length ? ` Chưa gán: ${result.failedContainerNumbers.join(', ')}. Vui lòng kiểm tra và thử lại.` : '');
}
