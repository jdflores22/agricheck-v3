import type { ApiEnvelope } from '../../auth/types'

type InitiateBillPaymentResponse = ApiEnvelope<{
  mode: string
  paymentReference: string
  paymentUrl?: string
  bill: {
    uuid: string
    billNumber: string
    description: string
    amount: number
    status: string
  }
}>

export async function redirectToBillPayment(
  initiate: (args: { uuid: string; paymentMethod: string }) => { unwrap: () => Promise<InitiateBillPaymentResponse> },
  billUuid: string,
  paymentMethod = 'card',
): Promise<'redirected' | 'completed' | 'failed'> {
  const result = await initiate({ uuid: billUuid, paymentMethod }).unwrap()

  if (result.success && result.data.paymentUrl) {
    window.location.href = result.data.paymentUrl
    return 'redirected'
  }

  if (result.success && result.data.mode === 'simulated' && result.data.bill.status === 'Paid') {
    return 'completed'
  }

  return 'failed'
}
