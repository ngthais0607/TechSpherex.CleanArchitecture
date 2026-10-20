import { useEffect, useMemo, useState, type ReactNode } from "react"

type Page = "dashboard" | "containers" | "gate" | "deliveryOrders" | "yard" | "customers" | "reports"
type IconName = "grid" | "box" | "gate" | "orders" | "map" | "users" | "chart" | "search" | "bell" | "plus" | "arrowUp" | "arrowDown" | "chevron" | "more" | "download" | "calendar" | "truck" | "check" | "menu" | "close" | "logout"
type ContainerType = "Dry" | "Reefer" | "Open Top" | "Flat Rack" | "Bunker" | "Ventilated" | "Specialized"
type ContainerSize = "20ft" | "40ft"
type Classification = "A" | "B" | "C"
type CheckState = {
  label: string
  state: "wait" | "pass" | "error"
  message: string
}
type Container = {
  code: string
  type: ContainerType
  iso: string
  size: ContainerSize
  operator: string
  owner: string
  classification: Classification
  condition: string
  maxWeight: number
  tareWeight: number
  manufactured: string
  location: string
  status: string
  date: string
  inYard: boolean
}
type OrderLine = {
  type: ContainerType
  size: ContainerSize
  quantity: number
  delivered: number
}
type DeliveryOrder = {
  number: string
  customer: string
  operator: string
  expiry: string
  vessel: string
  lines: OrderLine[]
}
type Placement = {
  code: string
  block: "A" | "B" | "C" | "D"
  slot: number
  row: number
  tier: number
}
type BlockConfig = {
  slots: number
  rows: number
  tiers: number
}
type GateRecord = {
  id: number
  date: string
  time: string
  code: string
  direction: "Nhập bãi" | "Xuất bãi"
  vehicleType: "Xe đầu kéo" | "Sà lan"
  vehicle: string
  driver: string
  operator: string
  classification: Classification
  condition: string
  location: string
  order: string
  status: "Hoàn tất" | "Đã hủy"
  changes: {
    time: string
    user: string
    field: string
    oldValue: string
    newValue: string
  }[]
}

const iconPaths: Record<IconName, ReactNode> = {
  grid: (
    <>
      <rect x="3" y="3" width="7" height="7" rx="1" />
      <rect x="14" y="3" width="7" height="7" rx="1" />
      <rect x="3" y="14" width="7" height="7" rx="1" />
      <rect x="14" y="14" width="7" height="7" rx="1" />
    </>
  ),
  box: (
    <>
      <path d="m21 8-9-5-9 5 9 5 9-5Z" />
      <path d="m3 8 9 5 9-5v9l-9 5-9-5V8Z" />
      <path d="M12 13v9" />
    </>
  ),
  gate: (
    <>
      <path d="M4 21V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v16" />
      <path d="M8 21V8h8v13M8 12h8M8 16h8" />
    </>
  ),
  orders: (
    <>
      <path d="M9 5H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-3" />
      <rect x="9" y="3" width="6" height="4" rx="1" />
      <path d="M8 12h8M8 16h5" />
    </>
  ),
  map: (
    <>
      <path d="m3 6 6-3 6 3 6-3v15l-6 3-6-3-6 3V6Z" />
      <path d="M9 3v15M15 6v15" />
    </>
  ),
  users: (
    <>
      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
    </>
  ),
  chart: (
    <>
      <path d="M3 3v18h18" />
      <path d="m7 16 4-5 4 3 5-7" />
    </>
  ),
  search: (
    <>
      <circle cx="11" cy="11" r="8" />
      <path d="m21 21-4.35-4.35" />
    </>
  ),
  bell: (
    <>
      <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
      <path d="M13.73 21a2 2 0 0 1-3.46 0" />
    </>
  ),
  plus: <path d="M12 5v14M5 12h14" />,
  arrowUp: <path d="m18 15-6-6-6 6" />,
  arrowDown: <path d="m6 9 6 6 6-6" />,
  chevron: <path d="m9 18 6-6-6-6" />,
  more: (
    <>
      <circle cx="5" cy="12" r="1" />
      <circle cx="12" cy="12" r="1" />
      <circle cx="19" cy="12" r="1" />
    </>
  ),
  download: (
    <>
      <path d="M12 3v12M7 10l5 5 5-5M5 21h14" />
    </>
  ),
  calendar: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M16 3v4M8 3v4M3 10h18" />
    </>
  ),
  truck: (
    <>
      <path d="M10 17h4V5H2v12h3" />
      <path d="M14 9h4l4 4v4h-3M14 17h1" />
      <circle cx="7.5" cy="17.5" r="2.5" />
      <circle cx="17.5" cy="17.5" r="2.5" />
    </>
  ),
  check: <path d="m5 12 4 4L19 6" />,
  menu: <path d="M4 6h16M4 12h16M4 18h16" />,
  close: <path d="m6 6 12 12M18 6 6 18" />,
  logout: (
    <>
      <path d="M10 17l5-5-5-5M15 12H3M21 19V5a2 2 0 0 0-2-2h-6" />
    </>
  ),
}
type IconProps = {
  name: IconName
  size?: number
}

function Icon({ name, size = 20 }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {iconPaths[name]}
    </svg>
  )
}

const navItems: {
  id: Page
  label: string
  icon: IconName
  extended?: boolean
}[] = [
  { id: "dashboard", label: "Tổng quan", icon: "grid" },
  { id: "containers", label: "Container", icon: "box" },
  { id: "gate", label: "Cổng nhập / xuất", icon: "gate" },
  { id: "deliveryOrders", label: "Lệnh giao", icon: "orders" },
  { id: "yard", label: "Sơ đồ bãi", icon: "map" },
  { id: "customers", label: "Khách hàng", icon: "users", extended: true },
  { id: "reports", label: "Báo cáo", icon: "chart", extended: true },
]
const operators = [
  {
    name: "Maersk",
    code: "MSK",
    contact: "operations.vn@maersk.com",
    containers: 384,
    status: "Hoạt động",
  },
  {
    name: "Evergreen",
    code: "EGL",
    contact: "depot.vn@evergreen.com",
    containers: 296,
    status: "Hoạt động",
  },
  {
    name: "CMA CGM",
    code: "CMA",
    contact: "ops.sgn@cma-cgm.com",
    containers: 215,
    status: "Hoạt động",
  },
  {
    name: "OOCL",
    code: "OOL",
    contact: "vietnam@oocl.com",
    containers: 162,
    status: "Hoạt động",
  },
  {
    name: "Hapag-Lloyd",
    code: "HLC",
    contact: "vn.depot@hlag.com",
    containers: 108,
    status: "Hoạt động",
  },
]
const customers = [
  {
    tax: "0312345678",
    name: "Công ty TNHH An Phát",
    contact: "logistics@anphat.vn",
    status: "Hoạt động",
  },
  {
    tax: "0309876543",
    name: "Công ty CP Hải Long",
    contact: "ops@hailong.vn",
    status: "Hoạt động",
  },
  {
    tax: "0301122334",
    name: "Công ty TNHH Việt Tín",
    contact: "xnk@viettin.vn",
    status: "Hoạt động",
  },
  {
    tax: "0315566778",
    name: "Công ty CP Nam Sơn",
    contact: "depot@namson.vn",
    status: "Hoạt động",
  },
]
const shippedContainers: Container[] = [
  {
    code: "MSKU 6020118",
    type: "Dry",
    iso: "22G1",
    size: "20ft",
    operator: "Maersk",
    owner: "MSK",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 30480,
    tareWeight: 2300,
    manufactured: "03/2020",
    location: "Ngoài bãi",
    status: "Đã xuất",
    date: "—",
    inYard: false,
  },
  {
    code: "MSKU 6020123",
    type: "Dry",
    iso: "22G1",
    size: "20ft",
    operator: "Maersk",
    owner: "MSK",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 30480,
    tareWeight: 2300,
    manufactured: "03/2020",
    location: "Ngoài bãi",
    status: "Đã xuất",
    date: "—",
    inYard: false,
  },
  {
    code: "MSKU 6020139",
    type: "Dry",
    iso: "45G1",
    size: "40ft",
    operator: "Maersk",
    owner: "MSK",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 32500,
    tareWeight: 3780,
    manufactured: "03/2020",
    location: "Ngoài bãi",
    status: "Đã xuất",
    date: "—",
    inYard: false,
  },
  {
    code: "EGHR 7103819",
    type: "Reefer",
    iso: "45R1",
    size: "40ft",
    operator: "Evergreen",
    owner: "EGH",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 32500,
    tareWeight: 4800,
    manufactured: "03/2020",
    location: "Ngoài bãi",
    status: "Đã xuất",
    date: "—",
    inYard: false,
  },
  {
    code: "CMAU 3401125",
    type: "Dry",
    iso: "22G1",
    size: "20ft",
    operator: "CMA CGM",
    owner: "CMA",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 30480,
    tareWeight: 2300,
    manufactured: "03/2020",
    location: "Ngoài bãi",
    status: "Đã xuất",
    date: "—",
    inYard: false,
  },
  {
    code: "HLXF 5208749",
    type: "Flat Rack",
    iso: "42P1",
    size: "40ft",
    operator: "Hapag-Lloyd",
    owner: "HLX",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 45000,
    tareWeight: 5200,
    manufactured: "03/2020",
    location: "Ngoài bãi",
    status: "Đã xuất",
    date: "—",
    inYard: false,
  },
]
const initialContainers: Container[] = [
  {
    code: "MSKU 4821904",
    type: "Dry",
    iso: "45G1",
    size: "40ft",
    operator: "Maersk",
    owner: "MSK",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 32500,
    tareWeight: 3780,
    manufactured: "06/2021",
    location: "A-02-A-2",
    status: "Trong bãi",
    date: "12/10/2026",
    inYard: true,
  },
  {
    code: "EGHU 6034211",
    type: "Dry",
    iso: "22G1",
    size: "20ft",
    operator: "Evergreen",
    owner: "EGH",
    classification: "B",
    condition: "Móp nhẹ",
    maxWeight: 30480,
    tareWeight: 2250,
    manufactured: "03/2020",
    location: "A-01-A-1",
    status: "Trong bãi",
    date: "10/10/2026",
    inYard: true,
  },
  {
    code: "CMAR 7710389",
    type: "Reefer",
    iso: "45R1",
    size: "40ft",
    operator: "CMA CGM",
    owner: "CMA",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 34000,
    tareWeight: 4650,
    manufactured: "11/2022",
    location: "A-06-B-1",
    status: "Đang cấp điện",
    date: "12/10/2026",
    inYard: true,
  },
  {
    code: "OOLS 2956106",
    type: "Open Top",
    iso: "22U1",
    size: "20ft",
    operator: "OOCL",
    owner: "OOL",
    classification: "C",
    condition: "Cong nhẹ",
    maxWeight: 30480,
    tareWeight: 2350,
    manufactured: "08/2019",
    location: "C-05-B-1",
    status: "Đang kiểm tra",
    date: "11/10/2026",
    inYard: true,
  },
  {
    code: "HLXF 8843619",
    type: "Flat Rack",
    iso: "42P1",
    size: "40ft",
    operator: "Hapag-Lloyd",
    owner: "HLX",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 45000,
    tareWeight: 5200,
    manufactured: "01/2023",
    location: "B-02-A-1",
    status: "Chờ xuất",
    date: "09/10/2026",
    inYard: true,
  },
  {
    code: "EGHU 3312474",
    type: "Dry",
    iso: "22G1",
    size: "20ft",
    operator: "Evergreen",
    owner: "EGH",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 30480,
    tareWeight: 2180,
    manufactured: "09/2021",
    location: "A-03-A-1",
    status: "Trong bãi",
    date: "08/10/2026",
    inYard: true,
  },
  {
    code: "MSCU 4821905",
    type: "Dry",
    iso: "45G1",
    size: "40ft",
    operator: "Maersk",
    owner: "MSC",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 32500,
    tareWeight: 3810,
    manufactured: "04/2022",
    location: "Ngoài bãi",
    status: "Chờ nhập",
    date: "—",
    inYard: false,
  },
  {
    code: "TGBU 4819208",
    type: "Dry",
    iso: "22G1",
    size: "20ft",
    operator: "Maersk",
    owner: "TGB",
    classification: "B",
    condition: "Trầy sơn",
    maxWeight: 30480,
    tareWeight: 2220,
    manufactured: "07/2020",
    location: "D-01-A-1",
    status: "Trong bãi",
    date: "12/10/2026",
    inYard: true,
  },
  {
    code: "CMAU 7621940",
    type: "Dry",
    iso: "45G1",
    size: "40ft",
    operator: "CMA CGM",
    owner: "CMA",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 32500,
    tareWeight: 3740,
    manufactured: "02/2021",
    location: "VB-01",
    status: "Chờ phân vị trí",
    date: "12/10/2026",
    inYard: true,
  },
  {
    code: "OOLU 3182077",
    type: "Ventilated",
    iso: "22V1",
    size: "20ft",
    operator: "OOCL",
    owner: "OOL",
    classification: "B",
    condition: "Bình thường",
    maxWeight: 30480,
    tareWeight: 2290,
    manufactured: "05/2020",
    location: "B-03-B-1",
    status: "Trong bãi",
    date: "11/10/2026",
    inYard: true,
  },
  {
    code: "ONEU 8319644",
    type: "Dry",
    iso: "45G1",
    size: "40ft",
    operator: "Ocean Network Express",
    owner: "ONE",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 32500,
    tareWeight: 3800,
    manufactured: "12/2021",
    location: "C-02-C-1",
    status: "Trong bãi",
    date: "10/10/2026",
    inYard: true,
  },
  {
    code: "HLCU 4091820",
    type: "Bunker",
    iso: "22B1",
    size: "20ft",
    operator: "Hapag-Lloyd",
    owner: "HLC",
    classification: "B",
    condition: "Bình thường",
    maxWeight: 30480,
    tareWeight: 2410,
    manufactured: "10/2018",
    location: "B-05-C-1",
    status: "Trong bãi",
    date: "09/10/2026",
    inYard: true,
  },
  {
    code: "MAEU 7284101",
    type: "Specialized",
    iso: "42S1",
    size: "40ft",
    operator: "Maersk",
    owner: "MAE",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 36000,
    tareWeight: 4900,
    manufactured: "03/2023",
    location: "VB-01",
    status: "Chờ phân vị trí",
    date: "12/10/2026",
    inYard: true,
  },
  {
    code: "SEGU 9146206",
    type: "Dry",
    iso: "22G1",
    size: "20ft",
    operator: "Evergreen",
    owner: "SEG",
    classification: "A",
    condition: "Bình thường",
    maxWeight: 30480,
    tareWeight: 2210,
    manufactured: "06/2022",
    location: "A-05-B-1",
    status: "Trong bãi",
    date: "12/10/2026",
    inYard: true,
  },
  ...shippedContainers,
]
const initialOrders: DeliveryOrder[] = [
  {
    number: "DO-2610-001",
    customer: "0312345678 – Công ty TNHH An Phát",
    operator: "Maersk",
    expiry: "30/10/2026",
    vessel: "Tàu A 0123E",
    lines: [
      { type: "Dry", size: "20ft", quantity: 10, delivered: 2 },
      { type: "Dry", size: "40ft", quantity: 5, delivered: 1 },
    ],
  },
  {
    number: "DO-2610-002",
    customer: "0309876543 – Công ty CP Hải Long",
    operator: "Evergreen",
    expiry: "15/10/2026",
    vessel: "Tàu B 0456W",
    lines: [
      { type: "Reefer", size: "40ft", quantity: 4, delivered: 1 },
      { type: "Dry", size: "20ft", quantity: 2, delivered: 0 },
    ],
  },
  {
    number: "DO-2610-003",
    customer: "0301122334 – Công ty TNHH Việt Tín",
    operator: "CMA CGM",
    expiry: "11/10/2026",
    vessel: "Tàu C 0789E",
    lines: [{ type: "Dry", size: "20ft", quantity: 8, delivered: 1 }],
  },
  {
    number: "DO-2610-004",
    customer: "0315566778 – Công ty CP Nam Sơn",
    operator: "OOCL",
    expiry: "05/11/2026",
    vessel: "Tàu D 0234E",
    lines: [
      { type: "Dry", size: "40ft", quantity: 12, delivered: 0 },
      { type: "Open Top", size: "20ft", quantity: 2, delivered: 0 },
    ],
  },
  {
    number: "DO-2610-005",
    customer: "0312345678 – Công ty TNHH An Phát",
    operator: "Hapag-Lloyd",
    expiry: "12/10/2026",
    vessel: "Tàu E 0312W",
    lines: [{ type: "Flat Rack", size: "40ft", quantity: 1, delivered: 1 }],
  },
]
const defaultBlocks: Record<"A" | "B" | "C" | "D", BlockConfig> = {
  A: { slots: 6, rows: 4, tiers: 5 },
  B: { slots: 5, rows: 3, tiers: 4 },
  C: { slots: 7, rows: 5, tiers: 3 },
  D: { slots: 4, rows: 2, tiers: 2 },
}
const placements: Placement[] = [
  { code: "EGHU 6034211", block: "A", slot: 1, row: 1, tier: 1 },
  { code: "EGHU 3312474", block: "A", slot: 2, row: 1, tier: 1 },
  { code: "MSKU 4821904", block: "A", slot: 1, row: 1, tier: 2 },
  { code: "CMAR 7710389", block: "A", slot: 3, row: 2, tier: 1 },
  { code: "SEGU 9146206", block: "A", slot: 3, row: 2, tier: 2 },
  { code: "HLXF 8843619", block: "B", slot: 1, row: 1, tier: 1 },
  { code: "OOLU 3182077", block: "B", slot: 3, row: 2, tier: 1 },
  { code: "HLCU 4091820", block: "B", slot: 5, row: 3, tier: 1 },
  { code: "ONEU 8319644", block: "C", slot: 1, row: 3, tier: 1 },
  { code: "OOLS 2956106", block: "C", slot: 3, row: 2, tier: 1 },
  { code: "TGBU 4819208", block: "D", slot: 1, row: 1, tier: 1 },
]
const initialHistory: GateRecord[] = [
  {
    id: 4,
    date: "12/10/2026",
    time: "09:32",
    code: "TGBU 4819208",
    direction: "Nhập bãi",
    vehicleType: "Xe đầu kéo",
    vehicle: "51D-284.19",
    driver: "Phạm Hải",
    operator: "Nguyễn Minh",
    classification: "B",
    condition: "Trầy sơn",
    location: "D-01-A-1",
    order: "—",
    status: "Hoàn tất",
    changes: [],
  },
  {
    id: 3,
    date: "12/10/2026",
    time: "09:18",
    code: "MSCU 4821905",
    direction: "Xuất bãi",
    vehicleType: "Xe đầu kéo",
    vehicle: "50H-109.72",
    driver: "Trần Hùng",
    operator: "Trần Nam",
    classification: "A",
    condition: "Bình thường",
    location: "A-02-A-1",
    order: "DO-2610-001",
    status: "Hoàn tất",
    changes: [],
  },
  {
    id: 2,
    date: "12/10/2026",
    time: "08:56",
    code: "CMAU 7621940",
    direction: "Nhập bãi",
    vehicleType: "Sà lan",
    vehicle: "SL-04",
    driver: "Võ Tấn",
    operator: "Nguyễn Minh",
    classification: "A",
    condition: "Bình thường",
    location: "VB-01",
    order: "—",
    status: "Hoàn tất",
    changes: [],
  },
  {
    id: 1,
    date: "12/10/2026",
    time: "08:41",
    code: "OOLU 3182077",
    direction: "Nhập bãi",
    vehicleType: "Xe đầu kéo",
    vehicle: "60C-193.28",
    driver: "Lê Long",
    operator: "Lê Hoàng",
    classification: "B",
    condition: "Bình thường",
    location: "B-03-B-1",
    order: "—",
    status: "Hoàn tất",
    changes: [],
  },
]

const normalizeCode = (value: string) =>
  value
    .toUpperCase()
    .replace(/\s/g, "")
    .replace(/^(.{4})(.{7})$/, "$1 $2")
function isValidContainerCode(value: string) {
  const raw = value.toUpperCase().replace(/\s/g, "")
  if (!/^[A-Z]{4}\d{7}$/.test(raw)) return false
  const map: Record<string, number> = {}
  let n = 10
  for (let code = 65; code <= 90; code += 1) {
    while (n % 11 === 0) n += 1
    map[String.fromCharCode(code)] = n
    n += 1
  }
  const sum = raw
    .slice(0, 10)
    .split("")
    .reduce(
      (total, char, index) =>
        total + (/[A-Z]/.test(char) ? map[char] : Number(char)) * 2 ** index,
      0,
    )
  return (sum % 11) % 10 === Number(raw[10])
}
const parseDate = (value: string) => {
  const [day, month, year] = value.split("/").map(Number)
  return new Date(year, month - 1, day)
}
const formatPosition = (p: Placement) =>
  `${p.block}-${String(initialContainers.find((c) => c.code === p.code)?.size === "40ft" ? p.slot * 2 : p.slot * 2 - 1).padStart(2, "0")}-${String.fromCharCode(64 + p.row)}-${p.tier}`
const orderStatus = (order: DeliveryOrder) => {
  const total = order.lines.reduce((s, line) => s + line.quantity, 0)
  const delivered = order.lines.reduce((s, line) => s + line.delivered, 0)
  if (delivered >= total) return "Đã giao đủ"
  const days = Math.ceil(
    (parseDate(order.expiry).getTime() - parseDate("12/10/2026").getTime()) /
      86400000,
  )
  return days < 0 ? "Hết hạn" : days <= 3 ? "Sắp hết hạn" : "Còn hạn"
}

type BookingStatus = "Đang giữ chỗ" | "Đã xuất" | "Đã hủy" | "Hết hạn"
type Booking = {
  id: number
  order: string
  customer: string
  operator: string
  code: string
  reserved: string
  shipped: string
  status: BookingStatus
}
const TODAY = "12/10/2026"
const seedBookings: Booking[] = [
  { id: 1, order: "DO-2610-001", customer: "0312345678 – Công ty TNHH An Phát", operator: "Maersk", code: "MSKU 4821904", reserved: "11/10/2026", shipped: "—", status: "Đang giữ chỗ" },
  { id: 2, order: "DO-2610-001", customer: "0312345678 – Công ty TNHH An Phát", operator: "Maersk", code: "TGBU 4819208", reserved: "12/10/2026", shipped: "—", status: "Đang giữ chỗ" },
  { id: 3, order: "DO-2610-004", customer: "0315566778 – Công ty CP Nam Sơn", operator: "OOCL", code: "OOLS 2956106", reserved: "12/10/2026", shipped: "—", status: "Đang giữ chỗ" },
  { id: 4, order: "DO-2610-002", customer: "0309876543 – Công ty CP Hải Long", operator: "Evergreen", code: "EGHU 3312474", reserved: "10/10/2026", shipped: "—", status: "Đang giữ chỗ" },
  { id: 5, order: "DO-2610-001", customer: "0312345678 – Công ty TNHH An Phát", operator: "Maersk", code: "MSKU 6020118", reserved: "04/10/2026", shipped: "07/10/2026", status: "Đã xuất" },
  { id: 6, order: "DO-2610-001", customer: "0312345678 – Công ty TNHH An Phát", operator: "Maersk", code: "MSKU 6020123", reserved: "05/10/2026", shipped: "08/10/2026", status: "Đã xuất" },
  { id: 7, order: "DO-2610-001", customer: "0312345678 – Công ty TNHH An Phát", operator: "Maersk", code: "MSKU 6020139", reserved: "06/10/2026", shipped: "09/10/2026", status: "Đã xuất" },
  { id: 8, order: "DO-2610-002", customer: "0309876543 – Công ty CP Hải Long", operator: "Evergreen", code: "EGHR 7103819", reserved: "03/10/2026", shipped: "06/10/2026", status: "Đã xuất" },
  { id: 9, order: "DO-2610-003", customer: "0301122334 – Công ty TNHH Việt Tín", operator: "CMA CGM", code: "CMAU 3401125", reserved: "02/10/2026", shipped: "05/10/2026", status: "Đã xuất" },
  { id: 10, order: "DO-2610-005", customer: "0312345678 – Công ty TNHH An Phát", operator: "Hapag-Lloyd", code: "HLXF 5208749", reserved: "01/10/2026", shipped: "04/10/2026", status: "Đã xuất" },
  { id: 11, order: "DO-2609-031", customer: "0309876543 – Công ty CP Hải Long", operator: "Maersk", code: "MSKU 4821904", reserved: "15/09/2026", shipped: "18/09/2026", status: "Đã xuất" },
  { id: 12, order: "DO-2608-022", customer: "0301122334 – Công ty TNHH Việt Tín", operator: "Maersk", code: "MSKU 4821904", reserved: "18/08/2026", shipped: "20/08/2026", status: "Đã xuất" },
  { id: 13, order: "DO-2609-018", customer: "0301122334 – Công ty TNHH Việt Tín", operator: "Maersk", code: "TGBU 4819208", reserved: "22/09/2026", shipped: "—", status: "Đã hủy" },
  { id: 14, order: "DO-2609-007", customer: "0315566778 – Công ty CP Nam Sơn", operator: "Evergreen", code: "EGHU 6034211", reserved: "30/08/2026", shipped: "02/09/2026", status: "Đã xuất" },
  { id: 15, order: "DO-2609-025", customer: "0312345678 – Công ty TNHH An Phát", operator: "Evergreen", code: "EGHU 6034211", reserved: "20/09/2026", shipped: "—", status: "Hết hạn" },
  { id: 16, order: "DO-2608-040", customer: "0309876543 – Công ty CP Hải Long", operator: "OOCL", code: "OOLS 2956106", reserved: "22/08/2026", shipped: "25/08/2026", status: "Đã xuất" },
  { id: 17, order: "DO-2609-011", customer: "0315566778 – Công ty CP Nam Sơn", operator: "Evergreen", code: "EGHU 3312474", reserved: "12/09/2026", shipped: "14/09/2026", status: "Đã xuất" },
]
type Store = { orders: DeliveryOrder[]; bookings: Booking[] }
const store: Store = { orders: initialOrders, bookings: seedBookings }
const subscribers = new Set<() => void>()
const setStore = (patch: Partial<Store>) => {
  Object.assign(store, patch)
  subscribers.forEach((notify) => notify())
}
function useStore() {
  const [, bump] = useState(0)
  useEffect(() => {
    const notify = () => bump((n) => n + 1)
    subscribers.add(notify)
    return () => {
      subscribers.delete(notify)
    }
  }, [])
  return store
}
let navigateTo: (page: Page, order?: string, filter?: string) => void =
  () => {}
let focusOrder: string | null = null
let focusFilter: string | null = null
const customerName = (value: string) => value.split(" – ")[1] ?? value
const orderByNo = (no: string) => store.orders.find((o) => o.number === no)
const containerByCode = (code: string) =>
  initialContainers.find((c) => c.code === code)
const effStatus = (b: Booking): BookingStatus => {
  const o = orderByNo(b.order)
  return b.status === "Đang giữ chỗ" && o && orderStatus(o) === "Hết hạn"
    ? "Hết hạn"
    : b.status
}
const bookingLabel = (b: Booking) => {
  const s = effStatus(b)
  const o = orderByNo(b.order)
  return s === "Đang giữ chỗ" && o && orderStatus(o) === "Sắp hết hạn"
    ? "Sắp hết hạn"
    : s
}
const activeBooking = (code: string) =>
  store.bookings.find((b) => b.code === code && effStatus(b) === "Đang giữ chỗ")
const historyOf = (code: string) =>
  store.bookings
    .filter((b) => b.code === code)
    .sort(
      (a, b) =>
        parseDate(b.reserved).getTime() - parseDate(a.reserved).getTime() ||
        b.id - a.id,
    )
const bookingSummary = (code: string) => {
  const hist = historyOf(code)
  const real = hist.filter((b) => b.status !== "Đã hủy")
  return {
    customers: new Set(real.map((b) => b.customer)).size,
    shipped: hist.filter((b) => b.status === "Đã xuất").length,
    latest: real[0] ? customerName(real[0].customer) : "—",
  }
}
const lineReserved = (order: DeliveryOrder, line: OrderLine) =>
  store.bookings.filter((b) => {
    const c = containerByCode(b.code)
    return (
      b.order === order.number &&
      effStatus(b) === "Đang giữ chỗ" &&
      !!c &&
      c.type === line.type &&
      c.size === line.size
    )
  }).length
const lineRemaining = (order: DeliveryOrder, line: OrderLine) =>
  line.quantity - line.delivered - lineReserved(order, line)
const eligibleContainers = (order: DeliveryOrder) =>
  initialContainers.filter(
    (c) =>
      c.inYard &&
      c.operator === order.operator &&
      !activeBooking(c.code) &&
      order.lines.some(
        (l) =>
          l.type === c.type && l.size === c.size && lineRemaining(order, l) > 0,
      ),
  )
const eligibleOrders = (c: Container) =>
  store.orders.filter(
    (o) =>
      o.operator === c.operator &&
      !["Hết hạn", "Đã giao đủ"].includes(orderStatus(o)) &&
      o.lines.some(
        (l) => l.type === c.type && l.size === c.size && lineRemaining(o, l) > 0,
      ),
  )
const assignBooking = (orderNo: string, codes: string[]) => {
  const o = orderByNo(orderNo)
  if (!o) return
  setStore({
    bookings: [
      ...codes.map((code, i) => ({
        id: Date.now() + i,
        order: o.number,
        customer: o.customer,
        operator: o.operator,
        code,
        reserved: TODAY,
        shipped: "—",
        status: "Đang giữ chỗ" as BookingStatus,
      })),
      ...store.bookings,
    ],
  })
}
const releaseBooking = (id: number) =>
  setStore({
    bookings: store.bookings.map((b) =>
      b.id === id ? { ...b, status: "Đã hủy" as BookingStatus } : b,
    ),
  })
const bookingTone = (c: Container) => {
  const b = activeBooking(c.code)
  if (!b) return "pale"
  return bookingLabel(b) === "Sắp hết hạn" ? "amber" : "cyan"
}
const yardOrderText = (code: string) => {
  const b = activeBooking(code)
  return b ? `${b.order} · ${customerName(b.customer)}` : "Chưa gán lệnh"
}
function BookingBadge({ label }: { label: string }) {
  const tone =
    label === "Đang giữ chỗ"
      ? "bg-cyan-50 text-cyan-700 ring-cyan-600/20"
      : label === "Đã xuất"
        ? "bg-emerald-50 text-emerald-700 ring-emerald-600/20"
        : label === "Sắp hết hạn"
          ? "bg-amber-50 text-amber-700 ring-amber-600/20"
          : label === "Hết hạn"
            ? "bg-rose-50 text-rose-700 ring-rose-600/20"
            : "bg-slate-100 text-slate-600 ring-slate-500/20"
  return (
    <span
      className={`inline-flex whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${tone}`}
    >
      {label}
    </span>
  )
}
function Button({
  children,
  variant = "primary",
  icon,
  onClick,
  disabled = false,
  type = "button",
}: {
  children: ReactNode
  variant?: "primary" | "secondary" | "ghost"
  icon?: IconName
  onClick?: () => void
  disabled?: boolean
  type?: "button" | "submit"
}) {
  const styles =
    variant === "primary"
      ? "bg-cyan-700 text-white hover:bg-cyan-800 shadow-sm"
      : variant === "secondary"
        ? "bg-white text-slate-700 border border-slate-200 hover:bg-slate-50"
        : "text-slate-500 hover:bg-slate-100"
  return (
    <button
      type={type}
      disabled={disabled}
      onClick={onClick}
      className={`inline-flex items-center justify-center gap-2 rounded-lg px-4 py-2.5 text-sm font-semibold transition ${styles} disabled:cursor-not-allowed disabled:opacity-40`}
    >
      {icon && <Icon name={icon} size={18} />}
      {children}
    </button>
  )
}
function StatusBadge({ value }: { value: string }) {
  const tone = [
    "Trong bãi",
    "Hoạt động",
    "Hoàn tất",
    "Còn hạn",
    "Bình thường",
    "Đã giao đủ",
  ].includes(value)
    ? "bg-emerald-50 text-emerald-700 ring-emerald-600/20"
    : ["Hết hạn", "Đã hủy"].includes(value)
      ? "bg-rose-50 text-rose-700 ring-rose-600/20"
      : ["Chờ xuất", "Nhập bãi"].includes(value)
        ? "bg-cyan-50 text-cyan-700 ring-cyan-600/20"
        : value === "Xuất bãi"
          ? "bg-violet-50 text-violet-700 ring-violet-600/20"
          : "bg-amber-50 text-amber-700 ring-amber-600/20"
  return (
    <span
      className={`inline-flex whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${tone}`}
    >
      {value}
    </span>
  )
}
function PageHeader({
  eyebrow,
  title,
  description,
  action,
}: {
  eyebrow: string
  title: string
  description: string
  action?: ReactNode
}) {
  return (
    <div className="mb-7 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <div className="mb-1.5 text-xs font-bold uppercase tracking-[0.16em] text-cyan-700">
          {eyebrow}
        </div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 md:text-[28px]">
          {title}
        </h1>
        <p className="mt-1.5 text-sm text-slate-500">{description}</p>
      </div>
      {action}
    </div>
  )
}
function MetricCard({
  label,
  value,
  trend,
  icon,
  tone,
}: {
  label: string
  value: string
  trend: string
  icon: IconName
  tone: "cyan" | "violet" | "amber" | "emerald"
}) {
  const colors = {
    cyan: "bg-cyan-50 text-cyan-700",
    violet: "bg-violet-50 text-violet-700",
    amber: "bg-amber-50 text-amber-700",
    emerald: "bg-emerald-50 text-emerald-700",
  }
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between">
        <div
          className={`grid size-10 place-items-center rounded-xl ${colors[tone]}`}
        >
          <Icon name={icon} />
        </div>
        <span className="inline-flex items-center gap-1 text-xs font-semibold text-emerald-600">
          <Icon name="arrowUp" size={14} />
          {trend}
        </span>
      </div>
      <div className="mt-5 text-3xl font-bold tracking-tight text-slate-900">
        {value}
      </div>
      <div className="mt-1 text-sm font-medium text-slate-500">{label}</div>
    </div>
  )
}
function Panel({
  title,
  subtitle,
  action,
  children,
  className = "",
}: {
  title: string
  subtitle?: string
  action?: ReactNode
  children: ReactNode
  className?: string
}) {
  return (
    <section
      className={`overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm ${className}`}
    >
      <div className="flex items-center justify-between gap-3 border-b border-slate-100 px-5 py-4">
        <div>
          <h2 className="font-bold text-slate-900">{title}</h2>
          {subtitle && (
            <p className="mt-0.5 text-xs text-slate-500">{subtitle}</p>
          )}
        </div>
        {action}
      </div>
      {children}
    </section>
  )
}
function InputField({
  label,
  value,
  onChange,
  placeholder = "",
  type = "text",
  icon,
  readOnly = false,
  error,
}: {
  label: string
  value?: string | number
  onChange?: (value: string) => void
  placeholder?: string
  type?: string
  icon?: IconName
  readOnly?: boolean
  error?: string
}) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-sm font-semibold text-slate-700">
        {label}
      </span>
      <span className="relative block">
        {icon && (
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
            <Icon name={icon} size={18} />
          </span>
        )}
        <input
          type={type}
          value={value}
          onChange={(e) => onChange?.(e.target.value)}
          placeholder={placeholder}
          readOnly={readOnly}
          className={`w-full rounded-lg border bg-white py-2.5 pr-3 text-sm outline-none focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100 ${
            icon ? "pl-10" : "pl-3"
          } ${error ? "border-rose-300" : "border-slate-200"} ${
            readOnly ? "bg-slate-50 text-slate-500" : ""
          }`}
        />
      </span>
      {error && (
        <span className="mt-1 block text-xs text-rose-600">{error}</span>
      )}
    </label>
  )
}
function SelectField({
  label,
  value,
  onChange,
  options,
  disabled = false,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  options: {
    value: string
    label: string
  }[]
  disabled?: boolean
}) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-sm font-semibold text-slate-700">
        {label}
      </span>
      <select
        disabled={disabled}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2.5 text-sm outline-none focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100 disabled:bg-slate-50"
      >
        <option value="">Chọn...</option>
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </label>
  )
}
function TableTools({
  placeholder,
  action,
}: {
  placeholder: string
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col gap-3 border-b border-slate-100 p-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="relative w-full sm:max-w-sm">
        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
          <Icon name="search" size={18} />
        </span>
        <input
          className="w-full rounded-lg border border-slate-200 bg-slate-50 py-2.5 pl-10 pr-3 text-sm outline-none focus:border-cyan-600 focus:bg-white focus:ring-2 focus:ring-cyan-100"
          placeholder={placeholder}
        />
      </div>
      {action}
    </div>
  )
}
function Modal({
  title,
  children,
  onClose,
  wide = false,
}: {
  title: string
  children: ReactNode
  onClose: () => void
  wide?: boolean
}) {
  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center overflow-y-auto bg-slate-950/40 p-4 backdrop-blur-sm"
      onMouseDown={onClose}
    >
      <div
        className={`my-6 w-full rounded-2xl bg-white shadow-2xl ${
          wide ? "max-w-4xl" : "max-w-2xl"
        }`}
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-5">
          <h2 className="text-lg font-bold text-slate-900">{title}</h2>
          <button
            onClick={onClose}
            className="rounded-lg p-2 text-slate-400 hover:bg-slate-100"
          >
            <Icon name="close" />
          </button>
        </div>
        <div className="p-6">{children}</div>
      </div>
    </div>
  )
}
type PaginationProps = {
  total: string
  count?: number
}

function Pagination({ total, count = 6 }: PaginationProps) {
  return (
    <div className="flex items-center justify-between border-t border-slate-100 px-5 py-4 text-sm">
      <span className="text-slate-500">
        Hiển thị 1–{count} / {total}
      </span>
      <div className="flex gap-1">
        <button className="rounded-md border border-slate-200 px-3 py-1.5 text-slate-400">
          Trước
        </button>
        <button className="rounded-md bg-slate-900 px-3 py-1.5 font-semibold text-white">
          1
        </button>
        <button className="rounded-md border border-slate-200 px-3 py-1.5 text-slate-600">
          2
        </button>
        <button className="rounded-md border border-slate-200 px-3 py-1.5 text-slate-600">
          Sau
        </button>
      </div>
    </div>
  )
}

function Dashboard({ navigate }: { navigate: (page: Page) => void }) {
  return (
    <>
      <PageHeader
        eyebrow="Trung tâm vận hành"
        title="Chào buổi sáng, Minh"
        description="Tình hình hoạt động tại Depot Cát Lái hôm nay, 12/10/2026."
        action={
          <Button icon="plus" onClick={() => navigate("gate")}>
            Tạo lệnh cổng
          </Button>
        }
      />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          label="Container trong bãi"
          value="1.248"
          trend="4,2%"
          icon="box"
          tone="cyan"
        />
        <MetricCard
          label="Nhập bãi hôm nay"
          value="86"
          trend="8,1%"
          icon="arrowDown"
          tone="violet"
        />
        <MetricCard
          label="Xuất bãi hôm nay"
          value="74"
          trend="2,4%"
          icon="arrowUp"
          tone="amber"
        />
        <MetricCard
          label="Tỷ lệ lấp đầy"
          value="72,4%"
          trend="1,6%"
          icon="chart"
          tone="emerald"
        />
      </div>
      <div className="mt-5 grid gap-5 xl:grid-cols-[1.55fr_1fr]">
        <Panel title="Lưu lượng qua cổng" subtitle="7 ngày gần nhất">
          <div className="p-5">
            <div className="mb-5 flex gap-5 text-xs font-medium text-slate-500">
              <span className="flex items-center gap-2">
                <i className="size-2.5 rounded-full bg-cyan-600" />
                Nhập bãi
              </span>
              <span className="flex items-center gap-2">
                <i className="size-2.5 rounded-full bg-violet-500" />
                Xuất bãi
              </span>
            </div>
            <div className="flex h-52 items-end justify-between gap-3 border-b border-slate-200">
              {[
                ["06/10", 58, 44],
                ["07/10", 72, 55],
                ["08/10", 50, 64],
                ["09/10", 82, 62],
                ["10/10", 68, 50],
                ["11/10", 91, 73],
                ["12/10", 76, 57],
              ].map(([day, incoming, outgoing]) => (
                <div
                  key={day}
                  className="flex h-full flex-1 flex-col justify-end"
                >
                  <div className="flex flex-1 items-end justify-center gap-1">
                    <div
                      className="w-[28%] rounded-t bg-cyan-600"
                      style={{ height: `${incoming}%` }}
                    />
                    <div
                      className="w-[28%] rounded-t bg-violet-400"
                      style={{ height: `${outgoing}%` }}
                    />
                  </div>
                  <span className="py-2 text-center text-[11px] text-slate-400">
                    {day}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </Panel>
        <Panel title="Công suất bãi" subtitle="Theo Block">
          <div className="space-y-5 p-5">
            {[
              ["Block A", 86, "1.032 / 1.200"],
              ["Block B", 74, "666 / 900"],
              ["Block C", 61, "488 / 800"],
              ["Block D", 48, "288 / 600"],
            ].map(([name, percent, count]) => (
              <div key={name as string}>
                <div className="mb-2 flex justify-between text-sm">
                  <span className="font-semibold text-slate-700">{name}</span>
                  <span className="text-slate-500">{count}</span>
                </div>
                <div className="h-2 rounded-full bg-slate-100">
                  <div
                    className={`h-2 rounded-full ${
                      percent as number > 80 ? "bg-amber-500" : "bg-cyan-600"
                    }`}
                    style={{ width: `${percent}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </Panel>
      </div>
      <Panel
        className="mt-5"
        title="Hoạt động gần đây"
        subtitle="Cập nhật theo thời gian thực"
      >
        <GateTable records={initialHistory} compact />
      </Panel>
    </>
  )
}

function ContainerDetails({
  item,
  onClose,
  initialTab = "info",
}: {
  item: Container
  onClose: () => void
  initialTab?: "info" | "orders"
}) {
  const [tab, setTab] = useState<"info" | "orders">(initialTab)
  const [pick, setPick] = useState("")
  useStore()
  const current = activeBooking(item.code)
  const currentOrder = current ? orderByNo(current.order) : undefined
  const history = historyOf(item.code)
  const summary = bookingSummary(item.code)
  const choices = eligibleOrders(item)
  const details = [
    ["Loại container", item.type],
    ["Mã ISO", item.iso],
    ["Kích thước", item.size],
    ["Hãng khai thác", item.operator],
    ["Chủ sở hữu", item.owner],
    ["Phân loại", item.classification],
    ["Tình trạng", item.condition],
    ["Trọng lượng tối đa", `${item.maxWeight.toLocaleString("vi-VN")} kg`],
    ["Trọng lượng vỏ", `${item.tareWeight.toLocaleString("vi-VN")} kg`],
    ["Ngày sản xuất", item.manufactured],
    ["Vị trí hiện tại", item.location],
  ]
  return (
    <Modal title={`Chi tiết ${item.code}`} onClose={onClose} wide>
      <div className="mb-5 inline-flex rounded-xl border border-slate-200 bg-slate-50 p-1">
        {(
          [
            ["info", "Thông tin"],
            ["orders", "Lệnh giao"],
          ] as const
        ).map(([id, label]) => (
          <button
            key={id}
            onClick={() => setTab(id)}
            className={`rounded-lg px-4 py-2 text-sm font-bold ${
              tab === id ? "bg-white text-slate-900 shadow-sm" : "text-slate-500"
            }`}
          >
            {label}
          </button>
        ))}
      </div>
      {tab === "info" ? (
        <div className="grid gap-3 sm:grid-cols-2">
          {details.map(([label, value]) => (
            <div
              key={label}
              className="rounded-xl border border-slate-200 bg-slate-50 p-3"
            >
              <div className="text-xs font-semibold text-slate-400">{label}</div>
              <div className="mt-1 font-bold text-slate-800">{value}</div>
            </div>
          ))}
        </div>
      ) : (
        <div className="space-y-5">
          <div className="rounded-xl border border-cyan-100 bg-cyan-50 p-4">
            <div className="mb-3 flex items-center justify-between">
              <div className="text-sm font-bold text-cyan-900">Lệnh hiện tại</div>
              {current && <BookingBadge label={bookingLabel(current)} />}
            </div>
            {current && currentOrder ? (
              <>
                <div className="grid gap-3 text-sm sm:grid-cols-2">
                  {[
                    ["Số lệnh", currentOrder.number],
                    ["Khách hàng (MST – tên)", currentOrder.customer],
                    ["Hãng khai thác", currentOrder.operator],
                    ["Hạn lệnh", currentOrder.expiry],
                    ["Chuyến tàu xuất", currentOrder.vessel],
                    ["Ngày giữ chỗ", current.reserved],
                  ].map(([label, value]) => (
                    <div key={label}>
                      <div className="text-xs font-semibold text-cyan-700">
                        {label}
                      </div>
                      <div className="font-bold text-slate-900">{value}</div>
                    </div>
                  ))}
                </div>
                <div className="mt-4">
                  <Button
                    variant="secondary"
                    onClick={() => {
                      onClose()
                      navigateTo("deliveryOrders", currentOrder.number)
                    }}
                  >
                    Xem lệnh
                  </Button>
                </div>
              </>
            ) : (
              <div>
                <div className="text-sm font-semibold text-cyan-900">
                  Container chưa được đặt chỗ
                </div>
                {item.inYard ? (
                  <>
                    <div className="mt-3 flex flex-wrap items-end gap-3">
                      <div className="min-w-64 flex-1">
                        <SelectField
                          label="Lệnh giao"
                          value={pick}
                          onChange={setPick}
                          options={choices.map((o) => ({
                            value: o.number,
                            label: `${o.number} · ${customerName(o.customer)} · hạn ${o.expiry}`,
                          }))}
                        />
                      </div>
                      <Button
                        disabled={!pick}
                        onClick={() => {
                          assignBooking(pick, [item.code])
                          setPick("")
                        }}
                      >
                        Gán vào lệnh giao
                      </Button>
                    </div>
                    {choices.length === 0 && (
                      <div className="mt-2 text-xs text-cyan-800">
                        Chưa có lệnh phù hợp: cùng hãng khai thác, còn hạn, còn
                        số lượng đúng loại và cỡ.
                      </div>
                    )}
                  </>
                ) : (
                  <div className="mt-2 text-xs text-cyan-800">
                    Container đang ngoài bãi nên chưa thể gán lệnh.
                  </div>
                )}
              </div>
            )}
          </div>
          <div className="rounded-xl border border-slate-200 bg-slate-50 p-3 text-sm font-semibold text-slate-700">
            Đã qua {summary.customers} khách hàng · Đã xuất {summary.shipped} lần
            · Khách đặt gần nhất: {summary.latest}
          </div>
          <div>
            <div className="mb-2 text-sm font-bold text-slate-800">
              Lịch sử lệnh giao
            </div>
            <div className="overflow-x-auto rounded-xl border border-slate-200">
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                    <th>Số lệnh</th>
                    <th>Khách hàng</th>
                    <th>Hãng khai thác</th>
                    <th>Ngày giữ chỗ</th>
                    <th>Ngày xuất</th>
                    <th>Trạng thái</th>
                  </tr>
                </thead>
                <tbody>
                  {history.map((b) => (
                    <tr key={b.id} className="border-t border-slate-100">
                      <td className="font-mono font-bold">{b.order}</td>
                      <td>{b.customer}</td>
                      <td>{b.operator}</td>
                      <td className="whitespace-nowrap">{b.reserved}</td>
                      <td className="whitespace-nowrap">{b.shipped}</td>
                      <td>
                        <BookingBadge label={effStatus(b)} />
                      </td>
                    </tr>
                  ))}
                  {history.length === 0 && (
                    <tr>
                      <td colSpan={6} className="text-slate-400">
                        Chưa có lệnh giao nào.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </Modal>
  )
}
function Containers() {
  const [items, setItems] = useState(initialContainers)
  const [assign, setAssign] = useState<"all" | "yes" | "no">("all")
  useStore()
  const [showForm, setShowForm] = useState(false)
  const [selected, setSelected] = useState<Container | null>(null)
  const emptyForm = {
    code: "",
    type: "Dry" as ContainerType,
    iso: "",
    size: "40ft" as ContainerSize,
    operator: "",
    classification: "A" as Classification,
    condition: "Bình thường",
    maxWeight: "",
    tareWeight: "",
    manufactured: "",
  }
  const [form, setForm] = useState(emptyForm)
  const shown = items.filter(
    (i) => assign === "all" || (assign === "yes") === !!activeBooking(i.code),
  )
  const owner = normalizeCode(form.code).replace(/\s/g, "").slice(0, 3)
  const errors = useMemo(() => {
    const result: Record<string, string> = {}
    if (!isValidContainerCode(form.code))
      result.code = "Số container sai định dạng hoặc số kiểm tra ISO 6346."
    if (!/^\d{2}[A-Z]\d$/.test(form.iso))
      result.iso = "Mã ISO phải có dạng 2 số + 1 chữ + 1 số, ví dụ 45G1."
    else if (
      (form.size === "20ft" && form.iso[0] !== "2") ||
      (form.size === "40ft" && form.iso[0] !== "4")
    )
      result.iso = `Chữ số đầu phải là ${
        form.size === "20ft" ? "2" : "4"
      } cho container ${form.size}.`
    if (
      Number(form.tareWeight) >= Number(form.maxWeight) ||
      !form.maxWeight ||
      !form.tareWeight
    )
      result.weight = "Trọng lượng vỏ phải nhỏ hơn trọng lượng tối đa."
    if (!/^\d{2}\/\d{4}$/.test(form.manufactured))
      result.manufactured = "Nhập tháng/năm theo MM/YYYY."
    else {
      const [m, y] = form.manufactured.split("/").map(Number)
      if (m < 1 || m > 12 || y > 2026 || (y === 2026 && m > 10))
        result.manufactured = "Ngày sản xuất không được ở tương lai."
    }
    if (!form.operator) result.operator = "Vui lòng chọn hãng khai thác."
    return result
  }, [form])
  const save = () => {
    if (Object.keys(errors).length) return
    setItems([
      {
        code: normalizeCode(form.code),
        type: form.type,
        iso: form.iso.toUpperCase(),
        size: form.size,
        operator: form.operator,
        owner,
        classification: form.classification,
        condition: form.condition,
        maxWeight: Number(form.maxWeight),
        tareWeight: Number(form.tareWeight),
        manufactured: form.manufactured,
        location: "Ngoài bãi",
        status: "Chờ nhập",
        date: "—",
        inYard: false,
      },
      ...items,
    ])
    setForm(emptyForm)
    setShowForm(false)
  }
  return (
    <>
      <PageHeader
        eyebrow="Quản lý tài sản"
        title="Danh sách container"
        description="Theo dõi đặc tính ISO, tình trạng và vị trí xếp bãi của từng container."
        action={
          <Button icon="plus" onClick={() => setShowForm(true)}>
            Thêm container
          </Button>
        }
      />
      <div className="mb-4 flex flex-wrap gap-2">
        {[
          "Tất cả 1.248",
          "Dry 842",
          "Reefer 186",
          "Open Top 92",
          "Flat Rack 74",
          "Bunker 32",
          "Ventilated 14",
          "Specialized 8",
        ].map((item, index) => (
          <button
            key={item}
            className={`rounded-lg px-3.5 py-2 text-sm font-semibold ${
              index === 0
                ? "bg-slate-900 text-white"
                : "border border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
            }`}
          >
            {item}
          </button>
        ))}
      </div>
      <div className="mb-4 inline-flex rounded-xl border border-slate-200 bg-white p-1 shadow-sm">
        {(
          [
            ["all", "Tất cả"],
            ["yes", "Đã gán lệnh"],
            ["no", "Chưa gán lệnh"],
          ] as const
        ).map(([id, label]) => (
          <button
            key={id}
            onClick={() => setAssign(id)}
            className={`rounded-lg px-3.5 py-2 text-sm font-semibold ${
              assign === id ? "bg-slate-900 text-white" : "text-slate-500"
            }`}
          >
            {label}
          </button>
        ))}
      </div>
      <Panel
        title="Thông tin container"
        subtitle="Bấm vào một dòng để xem đầy đủ thông tin kỹ thuật"
      >
        <TableTools
          placeholder="Tìm số container, mã ISO..."
          action={
            <div className="flex gap-2">
              <Button variant="secondary">Hãng khai thác</Button>
              <Button variant="secondary">Tình trạng</Button>
            </div>
          }
        />
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <th>Số container</th>
                <th>Loại · ISO</th>
                <th>Kích thước</th>
                <th>Hãng khai thác</th>
                <th>Tình trạng</th>
                <th>Vị trí</th>
                <th>Lệnh giao hiện tại</th>
                <th>Ngày nhập</th>
              </tr>
            </thead>
            <tbody>
              {shown.map((item) => (
                <tr
                  key={item.code}
                  onClick={() => setSelected(item)}
                  className="cursor-pointer border-t border-slate-100 hover:bg-slate-50/70"
                >
                  <td className="font-mono font-bold text-slate-900">
                    {item.code}
                  </td>
                  <td>
                    <div className="font-semibold text-slate-800">
                      {item.type}
                    </div>
                    <div className="font-mono text-xs text-slate-400">
                      {item.iso}
                    </div>
                  </td>
                  <td>
                    <span
                      className={`rounded-md px-2 py-1 text-xs font-bold ${
                        item.size === "40ft"
                          ? "bg-indigo-50 text-indigo-700"
                          : "bg-cyan-50 text-cyan-700"
                      }`}
                    >
                      {item.size}
                    </span>
                  </td>
                  <td>{item.operator}</td>
                  <td>
                    <StatusBadge value={item.condition} />
                  </td>
                  <td className="font-mono text-xs font-semibold">
                    {item.location}
                  </td>
                  <td className="min-w-52">
                    {(() => {
                      const b = activeBooking(item.code)
                      const o = b ? orderByNo(b.order) : undefined
                      return b && o ? (
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-mono text-xs font-bold">
                              {o.number}
                            </span>
                            <BookingBadge label={bookingLabel(b)} />
                          </div>
                          <div className="mt-1 max-w-56 truncate text-xs text-slate-600">
                            {customerName(o.customer)}
                          </div>
                          <div className="text-xs text-slate-400">
                            Hạn {o.expiry}
                          </div>
                        </div>
                      ) : (
                        <span className="text-xs text-slate-300">Chưa gán</span>
                      )
                    })()}
                  </td>
                  <td className="whitespace-nowrap text-slate-500">
                    {item.date}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination total="1.248 container" count={items.length} />
      </Panel>
      {selected && (
        <ContainerDetails item={selected} onClose={() => setSelected(null)} />
      )}
      {showForm && (
        <Modal title="Thêm container mới" onClose={() => setShowForm(false)}>
          <div className="mb-5 rounded-xl border border-cyan-100 bg-cyan-50 p-3 text-xs leading-5 text-cyan-800">
            Hệ thống kiểm tra định dạng và số kiểm tra ISO 6346 trước khi lưu.
          </div>
          <div className="mb-5 rounded-xl border border-slate-200 bg-slate-50 p-3 text-xs leading-5 text-slate-600">
            Lệnh giao được gán sau, ở màn hình Lệnh giao hoặc chi tiết container.
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <InputField
              label="Số container *"
              value={form.code}
              onChange={(code) => setForm({ ...form, code })}
              placeholder="VD: MSKU 4821904"
              error={form.code ? errors.code : ""}
            />
            <InputField
              label="Chủ sở hữu"
              value={owner}
              readOnly
              placeholder="Tự lấy từ số container"
            />
            <SelectField
              label="Loại container *"
              value={form.type}
              onChange={(type) =>
                setForm({ ...form, type: type as ContainerType })
              }
              options={[
                "Dry",
                "Reefer",
                "Open Top",
                "Flat Rack",
                "Bunker",
                "Ventilated",
                "Specialized",
              ].map((v) => ({ value: v, label: v }))}
            />
            <SelectField
              label="Kích thước *"
              value={form.size}
              onChange={(size) =>
                setForm({ ...form, size: size as ContainerSize })
              }
              options={["20ft", "40ft"].map((v) => ({ value: v, label: v }))}
            />
            <InputField
              label="Mã ISO *"
              value={form.iso}
              onChange={(iso) => setForm({ ...form, iso: iso.toUpperCase() })}
              placeholder="VD: 45G1"
              error={form.iso ? errors.iso : ""}
            />
            <SelectField
              label="Hãng khai thác *"
              value={form.operator}
              onChange={(operator) => setForm({ ...form, operator })}
              options={operators.map((o) => ({ value: o.name, label: o.name }))}
            />
            <SelectField
              label="Phân loại *"
              value={form.classification}
              onChange={(classification) =>
                setForm({
                  ...form,
                  classification: classification as Classification,
                })
              }
              options={["A", "B", "C"].map((v) => ({ value: v, label: v }))}
            />
            <InputField
              label="Tình trạng *"
              value={form.condition}
              onChange={(condition) => setForm({ ...form, condition })}
            />
            <InputField
              label="Trọng lượng tối đa (kg) *"
              type="number"
              value={form.maxWeight}
              onChange={(maxWeight) => setForm({ ...form, maxWeight })}
            />
            <InputField
              label="Trọng lượng vỏ (kg) *"
              type="number"
              value={form.tareWeight}
              onChange={(tareWeight) => setForm({ ...form, tareWeight })}
              error={form.tareWeight ? errors.weight : ""}
            />
            <InputField
              label="Ngày sản xuất (MM/YYYY) *"
              value={form.manufactured}
              onChange={(manufactured) => setForm({ ...form, manufactured })}
              placeholder="06/2021"
              error={form.manufactured ? errors.manufactured : ""}
            />
          </div>
          <div className="mt-6 flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setShowForm(false)}>
              Hủy
            </Button>
            <Button disabled={Object.keys(errors).length > 0} onClick={save}>
              Lưu container
            </Button>
          </div>
        </Modal>
      )}
    </>
  )
}

function GateTable({
  records,
  compact = false,
  onEdit,
  onCancel,
}: {
  records: GateRecord[]
  compact?: boolean
  onEdit?: (record: GateRecord) => void
  onCancel?: (record: GateRecord) => void
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
            <th>Thời gian</th>
            <th>Container</th>
            <th>Loại giao dịch</th>
            <th>Vị trí</th>
            <th>Số lệnh giao</th>
            <th>Loại phương tiện</th>
            <th>Phương tiện</th>
            {!compact && (
              <>
                <th>Trạng thái</th>
                <th />
              </>
            )}
          </tr>
        </thead>
        <tbody>
          {records.map((item) => (
            <tr
              key={item.id}
              className={`border-t border-slate-100 ${
                item.status === "Đã hủy" ? "opacity-60" : ""
              }`}
            >
              <td className="whitespace-nowrap font-semibold text-slate-900">
                {item.date}
                <br />
                <span className="text-xs text-slate-400">{item.time}</span>
              </td>
              <td className="font-mono font-bold">{item.code}</td>
              <td>
                <StatusBadge value={item.direction} />
              </td>
              <td className="font-mono text-xs">{item.location}</td>
              <td className="font-mono text-xs">{item.order}</td>
              <td>{item.vehicleType}</td>
              <td>{item.vehicle}</td>
              {!compact && (
                <>
                  <td>
                    <StatusBadge value={item.status} />
                  </td>
                  <td>
                    <div className="flex gap-2">
                      <button
                        disabled={item.status === "Đã hủy"}
                        onClick={() => onEdit?.(item)}
                        className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-bold text-slate-600 disabled:opacity-40"
                      >
                        Sửa
                      </button>
                      <button
                        disabled={item.status === "Đã hủy"}
                        onClick={() => onCancel?.(item)}
                        className="rounded-lg border border-rose-200 px-3 py-1.5 text-xs font-bold text-rose-600 disabled:opacity-40"
                      >
                        Hủy giao dịch
                      </button>
                    </div>
                  </td>
                </>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
function BusinessChecks({ checks }: { checks: CheckState[] }) {
  return (
    <Panel
      title="Kiểm tra nghiệp vụ"
      subtitle="Tự động đối chiếu theo dữ liệu đã nhập"
    >
      <div className="space-y-3 p-5">
        {checks.map((check) => (
          <div
            key={check.label}
            className={`flex items-center gap-3 rounded-xl border p-3 ${
              check.state === "error"
                ? "border-rose-100 bg-rose-50"
                : check.state === "pass"
                  ? "border-emerald-100 bg-emerald-50"
                  : "border-slate-100 bg-slate-50"
            }`}
          >
            <span
              className={`grid size-8 shrink-0 place-items-center rounded-full ${
                check.state === "error"
                  ? "bg-rose-100 text-rose-700"
                  : check.state === "pass"
                    ? "bg-emerald-100 text-emerald-700"
                    : "bg-slate-200 text-slate-500"
              }`}
            >
              {check.state === "pass" ? (
                <Icon name="check" size={16} />
              ) : check.state === "error" ? (
                <Icon name="close" size={16} />
              ) : (
                <Icon name="more" size={16} />
              )}
            </span>
            <div>
              <div className="text-sm font-semibold text-slate-700">
                {check.label}
              </div>
              <div
                className={`text-xs ${
                  check.state === "error" ? "text-rose-600" : "text-slate-500"
                }`}
              >
                {check.message}
              </div>
            </div>
          </div>
        ))}
      </div>
    </Panel>
  )
}
function Gate() {
  const [tab, setTab] = useState<"in" | "out" | "history">("in")
  const [records, setRecords] = useState(initialHistory)
  const orders = useStore().orders
  const setOrders = (next: DeliveryOrder[]) => setStore({ orders: next })
  const [editing, setEditing] = useState<GateRecord | null>(null)
  const empty = {
    code: "",
    vehicleType: "truck" as "truck" | "barge",
    vehicle: "",
    driver: "",
    classification: "" as "" | Classification,
    condition: "",
    block: "",
    bay: "",
    row: "",
    tier: "",
    order: "",
  }
  const [form, setForm] = useState(empty)
  const container = initialContainers.find(
    (c) => c.code === normalizeCode(form.code),
  )
  const booked = container ? activeBooking(container.code) : undefined
  const orderNo = form.order || booked?.order || ""
  const selectedOrder = orders.find((o) => o.number === orderNo)
  const block = form.block as "A" | "B" | "C" | "D"
  const config = defaultBlocks[block]
  const isVirtual = form.block === "VB-01"
  const positionError = useMemo(() => {
    if (tab !== "in") return ""
    if (!form.block) return "Chưa chọn Block."
    if (isVirtual) return ""
    if (!container || !config || !form.bay || !form.row || !form.tier)
      return "Chưa chọn đủ Bay, Row, Tier."
    const bay = Number(form.bay),
      row = form.row.charCodeAt(0) - 64,
      tier = Number(form.tier)
    if (tier > config.tiers)
      return `Tier tối đa của Block ${block} là ${config.tiers}.`
    if (container.size === "20ft" && bay % 2 === 0)
      return "Container 20ft phải xếp tại Bay lẻ."
    if (container.size === "40ft" && bay % 2 !== 0)
      return "Container 40ft phải xếp tại Bay chẵn."
    const slot = container.size === "40ft" ? bay / 2 : (bay + 1) / 2
    if (
      slot > config.slots ||
      (container.size === "40ft" && slot >= config.slots)
    )
      return "Vị trí vượt số bay lẻ của Block."
    const overlap = (p: Placement) => {
      const other = initialContainers.find((c) => c.code === p.code)
      const width = other?.size === "40ft" ? 2 : 1
      const targetWidth = container.size === "40ft" ? 2 : 1
      return (
        p.block === block &&
        p.row === row &&
        p.tier === tier &&
        p.slot < slot + targetWidth &&
        slot < p.slot + width
      )
    }
    if (placements.some(overlap)) return "Vị trí đã có container."
    if (tier > 1) {
      const support = placements.filter(
        (p) => p.block === block && p.row === row && p.tier === tier - 1,
      )
      const supported =
        container.size === "20ft"
          ? support.some(
              (p) =>
                p.slot === slot &&
                initialContainers.find((c) => c.code === p.code)?.size ===
                  "20ft",
            )
          : [slot, slot + 1].every((s) =>
              support.some((p) => {
                const other = initialContainers.find((c) => c.code === p.code)
                return other?.size === "40ft"
                  ? p.slot <= s && s < p.slot + 2
                  : p.slot === s
              }),
            )
      if (!supported)
        return container.size === "20ft"
          ? "20ft không được xếp lên 40ft hoặc vị trí không có đỡ tải."
          : "Tầng dưới chưa đỡ đủ hai slot cho container 40ft."
    }
    return ""
  }, [tab, form, container, config, block, isVirtual])
  const blocked = container
    ? placements.some((lower) => {
        if (lower.code !== container.code) return false
        const lowerWidth = container.size === "40ft" ? 2 : 1
        return placements.some((upper) => {
          const upperContainer = initialContainers.find(
            (c) => c.code === upper.code,
          )
          const upperWidth = upperContainer?.size === "40ft" ? 2 : 1
          return (
            upper.block === lower.block &&
            upper.row === lower.row &&
            upper.tier > lower.tier &&
            upper.slot < lower.slot + lowerWidth &&
            lower.slot < upper.slot + upperWidth
          )
        })
      })
    : false
  const checks: CheckState[] = [
    {
      label: "Định dạng và số kiểm tra",
      state: !form.code
        ? "wait"
        : isValidContainerCode(form.code)
          ? "pass"
          : "error",
      message: !form.code
        ? "Chờ nhập số container"
        : isValidContainerCode(form.code)
          ? "Đúng chuẩn ISO 6346"
          : "Sai định dạng hoặc số kiểm tra",
    },
    {
      label: "Container có trong danh mục",
      state: !form.code ? "wait" : container ? "pass" : "error",
      message: !form.code
        ? "Chờ đối chiếu"
        : container
          ? `${container.type} · ${container.iso} · ${container.operator}`
          : "Không tìm thấy trong danh mục container",
    },
    {
      label: "Trạng thái container",
      state: !container
        ? "wait"
        : tab === "in"
          ? !container.inYard
            ? "pass"
            : "error"
          : container.inYard
            ? "pass"
            : "error",
      message: !container
        ? "Chờ xác định container"
        : tab === "in"
          ? !container.inYard
            ? "Container chưa nằm trong bãi"
            : `Container đang ở ${container.location}`
          : container.inYard
            ? `Container đang ở ${container.location}`
            : "Container không nằm trong bãi",
    },
    {
      label: "Phương tiện",
      state: form.vehicle ? "pass" : "wait",
      message: form.vehicle
        ? `${
            form.vehicleType === "truck" ? "Xe đầu kéo" : "Sà lan"
          }: ${form.vehicle}`
        : "Chưa nhập biển số hoặc số hiệu",
    },
    ...(tab === "in"
      ? [
          {
            label: "Vị trí xếp",
            state: !form.block ? "wait" : positionError ? "error" : "pass",
            message: !form.block
              ? "Chờ chọn vị trí"
              : positionError ||
                (isVirtual
                  ? "Xếp vào Block ảo VB-01"
                  : `${form.block}-${form.bay}-${form.row}-${form.tier} hợp lệ`),
          } as CheckState,
        ]
      : [
          {
            label: "Lệnh giao",
            state:
              !selectedOrder || !container
                ? "wait"
                : booked && booked.order !== selectedOrder.number
                  ? "error"
                  : orderStatus(selectedOrder) === "Hết hạn"
                  ? "error"
                  : selectedOrder.operator !== container.operator
                    ? "error"
                    : !selectedOrder.lines.some(
                          (l) =>
                            l.type === container.type &&
                            l.size === container.size &&
                            (booked ? l.delivered < l.quantity : lineRemaining(selectedOrder, l) > 0),
                        )
                      ? "error"
                      : "pass",
            message: !selectedOrder
              ? "Chưa chọn lệnh giao"
              : !container
                ? "Chờ xác định container"
                : booked && booked.order !== selectedOrder.number
                  ? `Container đã giữ chỗ cho lệnh ${booked.order}`
                  : orderStatus(selectedOrder) === "Hết hạn"
                  ? `Lệnh hết hạn ngày ${selectedOrder.expiry}`
                  : selectedOrder.operator !== container.operator
                    ? `Lệnh thuộc ${selectedOrder.operator}, container thuộc ${container.operator}`
                    : selectedOrder.lines.some(
                          (l) =>
                            l.type === container.type &&
                            l.size === container.size &&
                            (booked ? l.delivered < l.quantity : lineRemaining(selectedOrder, l) > 0),
                        )
                      ? "Còn số lượng phù hợp"
                      : `Đã hết số lượng ${container.size} ${container.type}`,
          } as CheckState,
          {
            label: "Container có bị đè",
            state: !container ? "wait" : blocked ? "error" : "pass",
            message: !container
              ? "Chờ xác định vị trí"
              : blocked
                ? "Còn container ở tầng trên, chưa thể xuất"
                : "Không có container đè phía trên",
          } as CheckState,
        ]),
  ]
  const allPass =
    checks.every((c) => c.state === "pass") &&
    !!form.classification &&
    !!form.condition &&
    !!form.driver
  const suggest = () => {
    if (!container) return
    if (container.size === "40ft")
      setForm({ ...form, block: "D", bay: "02", row: "B", tier: "1" })
    else setForm({ ...form, block: "A", bay: "07", row: "A", tier: "1" })
  }
  const submit = () => {
    if (!allPass || !container) return
    const location =
      tab === "in"
        ? isVirtual
          ? "VB-01"
          : `${form.block}-${form.bay}-${form.row}-${form.tier}`
        : container.location
    setRecords([
      {
        id: Date.now(),
        date: "12/10/2026",
        time: "10:06",
        code: container.code,
        direction: tab === "in" ? "Nhập bãi" : "Xuất bãi",
        vehicleType: form.vehicleType === "truck" ? "Xe đầu kéo" : "Sà lan",
        vehicle: form.vehicle,
        driver: form.driver,
        operator: "Nguyễn Minh",
        classification: form.classification as Classification,
        condition: form.condition,
        location,
        order: tab === "out" ? form.order : "—",
        status: "Hoàn tất",
        changes: [],
      },
      ...records,
    ])
    if (tab === "out" && selectedOrder)
      setOrders(
        orders.map((o) =>
          o.number !== selectedOrder.number
            ? o
            : {
                ...o,
                lines: o.lines.map((l) =>
                  l.type === container.type && l.size === container.size
                    ? { ...l, delivered: l.delivered + 1 }
                    : l,
                ),
              },
        ),
      )
    if (tab === "out" && selectedOrder) {
      const b = activeBooking(container.code)
      if (b)
        setStore({
          bookings: store.bookings.map((x) =>
            x.id === b.id
              ? { ...x, status: "Đã xuất" as BookingStatus, shipped: TODAY }
              : x,
          ),
        })
      else
        setStore({
          bookings: [
            {
              id: Date.now(),
              order: selectedOrder.number,
              customer: selectedOrder.customer,
              operator: selectedOrder.operator,
              code: container.code,
              reserved: TODAY,
              shipped: TODAY,
              status: "Đã xuất" as BookingStatus,
            },
            ...store.bookings,
          ],
        })
    }
    setForm(empty)
    setTab("history")
  }
  const cancel = (record: GateRecord) => {
    const latest = records.find(
      (r) => r.code === record.code && r.status === "Hoàn tất",
    )
    if (latest?.id !== record.id) {
      window.alert("Chỉ có thể hủy giao dịch gần nhất của container này.")
      return
    }
    if (
      !window.confirm(
        `Hủy giao dịch ${record.direction.toLowerCase()} của ${record.code}?`,
      )
    )
      return
    setRecords(
      records.map((r) => (r.id === record.id ? { ...r, status: "Đã hủy" } : r)),
    )
    if (record.direction === "Xuất bãi" && record.order !== "—") {
      const c = initialContainers.find((item) => item.code === record.code)
      const sb = store.bookings.find(
        (x) =>
          x.code === record.code &&
          x.order === record.order &&
          x.status === "Đã xuất",
      )
      const ord = orderByNo(record.order)
      if (sb)
        setStore({
          bookings: store.bookings.map((x) =>
            x.id === sb.id
              ? {
                  ...x,
                  status: (ord && orderStatus(ord) === "Hết hạn"
                    ? "Hết hạn"
                    : "Đang giữ chỗ") as BookingStatus,
                  shipped: "—",
                }
              : x,
          ),
        })
      setOrders(
        orders.map((o) =>
          o.number !== record.order
            ? o
            : {
                ...o,
                lines: o.lines.map((l) =>
                  c && l.type === c.type && l.size === c.size
                    ? { ...l, delivered: Math.max(0, l.delivered - 1) }
                    : l,
                ),
              },
        ),
      )
      window.alert(
        `Đã hoàn lại số lượng cho ${record.order} và trả container về ${record.location}.`,
      )
    }
  }
  return (
    <>
      <PageHeader
        eyebrow="Điều phối cổng"
        title="Ghi nhận nhập / xuất cổng"
        description="Kiểm tra container, phương tiện, lệnh giao và vị trí trước khi hoàn tất giao dịch."
      />
      <div className="mb-5 inline-flex rounded-xl border border-slate-200 bg-white p-1 shadow-sm">
        {[
          ["in", "Nhập bãi"],
          ["out", "Xuất bãi"],
          ["history", "Lịch sử"],
        ].map(([id, label]) => (
          <button
            key={id}
            onClick={() => {
              setTab(id as typeof tab)
              setForm(empty)
            }}
            className={`rounded-lg px-5 py-2.5 text-sm font-semibold ${
              tab === id ? "bg-slate-900 text-white" : "text-slate-500"
            }`}
          >
            {label}
          </button>
        ))}
      </div>
      {tab === "history" ? (
        <Panel
          title="Lịch sử giao dịch hôm nay"
          subtitle="Dữ liệu mẫu ngày 12/10/2026"
        >
          <TableTools
            placeholder="Tìm container hoặc phương tiện..."
            action={
              <Button variant="secondary" icon="calendar">
                12/10/2026
              </Button>
            }
          />
          <GateTable records={records} onEdit={setEditing} onCancel={cancel} />
          <Pagination
            total={`${records.length} giao dịch`}
            count={records.length}
          />
        </Panel>
      ) : (
        <div className="grid gap-5 xl:grid-cols-[1.6fr_1fr]">
          <Panel
            title={tab === "in" ? "Phiếu nhập bãi" : "Phiếu xuất bãi"}
            subtitle="Các trường có dấu * là bắt buộc"
          >
            {tab === "out" && (
              <div className="border-b border-slate-100 p-5">
                <div className="mb-4 text-xs font-bold uppercase tracking-wider text-slate-400">
                  1. Lệnh giao
                </div>
                  <div>
                    <SelectField
                      label="Số lệnh giao *"
                      value={orderNo}
                      onChange={(order) => setForm({ ...form, order })}
                      options={orders.map((o) => ({
                        value: o.number,
                        label: `${o.number} · ${o.operator} · ${o.expiry} · ${orderStatus(o)}`,
                      }))}
                    />
                    {booked && (
                      <div className="mt-3 rounded-xl border border-amber-200 bg-amber-50 p-3 text-xs font-semibold text-amber-800">
                        Container đã giữ chỗ cho lệnh {booked.order} (khách:{" "}
                        {customerName(booked.customer)})
                      </div>
                    )}
                    {selectedOrder && (
                      <div className="mt-3 grid gap-2 rounded-xl border border-cyan-100 bg-cyan-50 p-3 text-xs text-cyan-900 sm:grid-cols-3">
                        <span>
                          <b>Khách hàng:</b>
                          <br />
                          {selectedOrder.customer}
                        </span>
                        <span>
                          <b>Chuyến tàu:</b>
                          <br />
                          {selectedOrder.vessel}
                        </span>
                        <span>
                          <b>Còn lại:</b>
                          <br />
                          {selectedOrder.lines
                            .map(
                              (l) =>
                                `${l.size} ${l.type}: ${l.quantity - l.delivered}`,
                            )
                            .join(" · ")}
                        </span>
                      </div>
                    )}
                  </div>
              </div>
            )}
            <div className="border-b border-slate-100 p-5">
              <div className="mb-4 text-xs font-bold uppercase tracking-wider text-slate-400">
                {tab === "out" ? 2 : 1}. Container
              </div>
              <div className="grid gap-4 sm:grid-cols-2">
                <InputField
                  label="Số container *"
                  value={form.code}
                  onChange={(code) => setForm({ ...form, code })}
                  placeholder="VD: MSCU 4821905"
                  icon="box"
                />
                <InputField
                  label="Hãng khai thác"
                  value={container?.operator || "Tự động theo danh mục"}
                  readOnly
                />
              </div>
            </div>
            <div className="border-b border-slate-100 p-5">
              <div className="mb-4 text-xs font-bold uppercase tracking-wider text-slate-400">
                {tab === "out" ? 3 : 2}. Phương tiện vận chuyển
              </div>
              <div className="mb-4 flex gap-2">
                {[
                  ["truck", "Xe đầu kéo"],
                  ["barge", "Sà lan"],
                ].map(([id, label]) => (
                  <button
                    key={id}
                    onClick={() =>
                      setForm({
                        ...form,
                        vehicleType: id as "truck" | "barge",
                        vehicle: "",
                      })
                    }
                    className={`flex-1 rounded-lg border px-4 py-2.5 text-sm font-semibold ${
                      form.vehicleType === id
                        ? "border-cyan-600 bg-cyan-50 text-cyan-800"
                        : "border-slate-200 text-slate-500"
                    }`}
                  >
                    {label}
                  </button>
                ))}
              </div>
              <div className="grid gap-4 sm:grid-cols-2">
                <InputField
                  label={
                    form.vehicleType === "truck"
                      ? "Biển số xe *"
                      : "Số hiệu sà lan *"
                  }
                  value={form.vehicle}
                  onChange={(vehicle) => setForm({ ...form, vehicle })}
                  icon="truck"
                />
                <InputField
                  label={
                    form.vehicleType === "truck"
                      ? "Tài xế *"
                      : "Người vận hành *"
                  }
                  value={form.driver}
                  onChange={(driver) => setForm({ ...form, driver })}
                />
              </div>
            </div>
            <div className="p-5">
              <div className="mb-4 text-xs font-bold uppercase tracking-wider text-slate-400">
                {tab === "out" ? 4 : 3}. Kiểm tra & điều phối
              </div>
              <div className="grid gap-4 sm:grid-cols-2">
                <SelectField
                  label="Phân loại container *"
                  value={form.classification}
                  onChange={(classification) =>
                    setForm({
                      ...form,
                      classification: classification as Classification,
                    })
                  }
                  options={["A", "B", "C"].map((v) => ({ value: v, label: v }))}
                />
                <InputField
                  label="Tình trạng *"
                  value={form.condition}
                  onChange={(condition) => setForm({ ...form, condition })}
                  placeholder="Bình thường, móp, cong..."
                />
                {tab === "out" ? (
                  null
                ) : (
                  <>
                    <SelectField
                      label="Block dự kiến *"
                      value={form.block}
                      onChange={(blockValue) =>
                        setForm({
                          ...form,
                          block: blockValue,
                          bay: "",
                          row: "",
                          tier: "",
                        })
                      }
                      options={["A", "B", "C", "D", "VB-01"].map((v) => ({
                        value: v,
                        label: v === "VB-01" ? "Block ảo VB-01" : `Block ${v}`,
                      }))}
                    />
                    <div className="flex items-end">
                      <Button
                        variant="secondary"
                        onClick={suggest}
                        disabled={!container}
                      >
                        Gợi ý vị trí trống
                      </Button>
                    </div>
                    {!isVirtual && (
                      <>
                        <SelectField
                          label="Bay *"
                          value={form.bay}
                          onChange={(bay) => setForm({ ...form, bay })}
                          disabled={!config}
                          options={
                            config
                              ? Array.from(
                                  { length: config.slots * 2 - 1 },
                                  (_, i) => i + 1,
                                )
                                  .filter((n) =>
                                    container?.size === "40ft"
                                      ? n % 2 === 0
                                      : n % 2 === 1,
                                  )
                                  .map((n) => ({
                                    value: String(n).padStart(2, "0"),
                                    label: `Bay ${String(n).padStart(2, "0")}`,
                                  }))
                              : []
                          }
                        />
                        <SelectField
                          label="Row *"
                          value={form.row}
                          onChange={(row) => setForm({ ...form, row })}
                          disabled={!config}
                          options={
                            config
                              ? Array.from({ length: config.rows }, (_, i) => ({
                                  value: String.fromCharCode(65 + i),
                                  label: `Row ${String.fromCharCode(65 + i)}`,
                                }))
                              : []
                          }
                        />
                        <SelectField
                          label="Tier *"
                          value={form.tier}
                          onChange={(tier) => setForm({ ...form, tier })}
                          disabled={!config}
                          options={
                            config
                              ? Array.from(
                                  { length: config.tiers },
                                  (_, i) => ({
                                    value: String(i + 1),
                                    label: `Tier ${i + 1}`,
                                  }),
                                )
                              : []
                          }
                        />
                      </>
                    )}
                  </>
                )}
              </div>
            </div>
            <div className="flex justify-end gap-3 border-t border-slate-100 px-5 py-4">
              <Button variant="secondary" onClick={() => setForm(empty)}>
                Xóa nội dung
              </Button>
              <Button disabled={!allPass} onClick={submit}>
                {tab === "in" ? "Ghi nhận nhập bãi" : "Ghi nhận xuất bãi"}
              </Button>
            </div>
          </Panel>
          <div className="space-y-5">
            <BusinessChecks checks={checks} />
            {!allPass && (
              <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4 text-sm leading-6 text-amber-900">
                Nút Ghi nhận chỉ được bật khi mọi kiểm tra đều đạt và đã nhập đủ
                phân loại, tình trạng, người điều khiển.
              </div>
            )}
          </div>
        </div>
      )}
      {editing && (
        <EditGateRecord
          record={editing}
          onClose={() => setEditing(null)}
          onSave={(updated) => {
            setRecords(records.map((r) => (r.id === updated.id ? updated : r)))
            setEditing(null)
          }}
        />
      )}
    </>
  )
}
function EditGateRecord({
  record,
  onClose,
  onSave,
}: {
  record: GateRecord
  onClose: () => void
  onSave: (record: GateRecord) => void
}) {
  const [draft, setDraft] = useState(record)
  const save = () => {
    const fields: [keyof GateRecord, string][] = [
      ["vehicleType", "Loại phương tiện"],
      ["vehicle", "Phương tiện"],
      ["driver", "Tài xế / người vận hành"],
      ["classification", "Phân loại"],
      ["condition", "Tình trạng"],
    ]
    const changes = fields
      .filter(([key]) => draft[key] !== record[key])
      .map(([key, field]) => ({
        time: "12/10/2026 10:12",
        user: "Nguyễn Minh",
        field,
        oldValue: String(record[key]),
        newValue: String(draft[key]),
      }))
    onSave({ ...draft, changes: [...changes, ...record.changes] })
  }
  return (
    <Modal title="Cập nhật bản ghi cổng" onClose={onClose} wide>
      <div className="mb-5 rounded-xl border border-amber-200 bg-amber-50 p-3 text-xs text-amber-800">
        Không thể sửa số container và loại giao dịch. Nếu nhập sai, hãy hủy giao
        dịch rồi tạo lại.
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <InputField label="Số container" value={record.code} readOnly />
        <InputField label="Loại giao dịch" value={record.direction} readOnly />
        <SelectField
          label="Loại phương tiện"
          value={draft.vehicleType}
          onChange={(vehicleType) =>
            setDraft({
              ...draft,
              vehicleType: vehicleType as GateRecord["vehicleType"],
            })
          }
          options={["Xe đầu kéo", "Sà lan"].map((v) => ({
            value: v,
            label: v,
          }))}
        />
        <InputField
          label="Phương tiện"
          value={draft.vehicle}
          onChange={(vehicle) => setDraft({ ...draft, vehicle })}
        />
        <InputField
          label="Tài xế / người vận hành"
          value={draft.driver}
          onChange={(driver) => setDraft({ ...draft, driver })}
        />
        <SelectField
          label="Phân loại"
          value={draft.classification}
          onChange={(classification) =>
            setDraft({
              ...draft,
              classification: classification as Classification,
            })
          }
          options={["A", "B", "C"].map((v) => ({ value: v, label: v }))}
        />
        <InputField
          label="Tình trạng"
          value={draft.condition}
          onChange={(condition) => setDraft({ ...draft, condition })}
        />
      </div>
      <div className="mt-6">
        <div className="mb-2 text-sm font-bold text-slate-800">
          Lịch sử thay đổi
        </div>
        <div className="overflow-x-auto rounded-xl border border-slate-200">
          <table className="w-full text-left text-xs">
            <thead>
              <tr className="bg-slate-50 text-slate-500">
                <th>Thời gian</th>
                <th>Người sửa</th>
                <th>Trường</th>
                <th>Giá trị cũ</th>
                <th>Giá trị mới</th>
              </tr>
            </thead>
            <tbody>
              {record.changes.length ? (
                record.changes.map((c, i) => (
                  <tr key={i} className="border-t border-slate-100">
                    <td>{c.time}</td>
                    <td>{c.user}</td>
                    <td>{c.field}</td>
                    <td>{c.oldValue}</td>
                    <td>{c.newValue}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={5} className="text-center text-slate-400">
                    Chưa có thay đổi
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      <div className="mt-6 flex justify-end gap-3">
        <Button variant="secondary" onClick={onClose}>
          Hủy
        </Button>
        <Button onClick={save}>Lưu thay đổi</Button>
      </div>
    </Modal>
  )
}

function OrderDetail({
  order,
  onClose,
  initialPicking = false,
}: {
  order: DeliveryOrder
  onClose: () => void
  initialPicking?: boolean
}) {
  useStore()
  const [picking, setPicking] = useState(initialPicking)
  const [selection, setSelection] = useState<string[]>([])
  const status = orderStatus(order)
  const assigned = store.bookings.filter(
    (b) => b.order === order.number && b.status !== "Đã hủy",
  )
  const candidates = eligibleContainers(order)
  const selectedFor = (l: OrderLine) =>
    selection.filter((code) => {
      const c = containerByCode(code)
      return !!c && c.type === l.type && c.size === l.size
    }).length
  const canTake = (c: Container) => {
    if (selection.includes(c.code)) return true
    const l = order.lines.find((x) => x.type === c.type && x.size === c.size)
    return !!l && lineRemaining(order, l) - selectedFor(l) > 0
  }
  const toggle = (code: string) =>
    setSelection(
      selection.includes(code)
        ? selection.filter((x) => x !== code)
        : [...selection, code],
    )
  const confirmAssign = () => {
    assignBooking(order.number, selection)
    setSelection([])
    setPicking(false)
  }
  const release = (b: Booking) => {
    if (window.confirm(`Gỡ ${b.code} khỏi lệnh ${order.number}?`))
      releaseBooking(b.id)
  }
  const locked = status === "Hết hạn" || status === "Đã giao đủ"
  return (
    <>
      <Modal title={`Chi tiết lệnh ${order.number}`} onClose={onClose} wide>
        <div className="grid gap-3 sm:grid-cols-3">
          {[
            ["Khách hàng (MST – tên)", order.customer],
            ["Hãng khai thác", order.operator],
            ["Hạn lệnh", order.expiry],
            ["Chuyến tàu xuất", order.vessel],
          ].map(([label, value]) => (
            <div
              key={label}
              className="rounded-xl border border-slate-200 bg-slate-50 p-3"
            >
              <div className="text-xs font-semibold text-slate-400">{label}</div>
              <div className="mt-1 font-bold text-slate-800">{value}</div>
            </div>
          ))}
          <div className="rounded-xl border border-slate-200 bg-slate-50 p-3">
            <div className="text-xs font-semibold text-slate-400">Trạng thái</div>
            <div className="mt-1">
              <StatusBadge value={status} />
            </div>
          </div>
        </div>
        <div className="mt-6 mb-2 text-sm font-bold">Số lượng theo loại</div>
        <div className="overflow-x-auto rounded-xl border border-slate-200">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <th>Loại · Cỡ</th>
                <th>Tổng</th>
                <th>Đã giữ chỗ</th>
                <th>Đã xuất</th>
                <th>Còn lại</th>
              </tr>
            </thead>
            <tbody>
              {order.lines.map((l, i) => (
                <tr key={i} className="border-t border-slate-100">
                  <td className="font-semibold">
                    {l.type} · {l.size}
                  </td>
                  <td>{l.quantity}</td>
                  <td>{lineReserved(order, l)}</td>
                  <td>{l.delivered}</td>
                  <td className="font-bold">{lineRemaining(order, l)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="mt-6 mb-2 flex items-center justify-between">
          <div className="text-sm font-bold">Container đã gán</div>
          <Button
            icon="plus"
            disabled={locked}
            onClick={() => setPicking(true)}
          >
            Gán container
          </Button>
        </div>
        <div className="overflow-x-auto rounded-xl border border-slate-200">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <th>Số container</th>
                <th>Loại · ISO</th>
                <th>Cỡ</th>
                <th>Vị trí</th>
                <th>Trạng thái</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {assigned.map((b) => {
                const c = containerByCode(b.code)
                return (
                  <tr key={b.id} className="border-t border-slate-100">
                    <td className="font-mono font-bold">{b.code}</td>
                    <td>
                      {c?.type} · {c?.iso}
                    </td>
                    <td>{c?.size}</td>
                    <td className="font-mono text-xs font-semibold">
                      {c?.location}
                    </td>
                    <td>
                      <BookingBadge label={effStatus(b)} />
                    </td>
                    <td className="text-right">
                      {effStatus(b) === "Đang giữ chỗ" && (
                        <Button variant="ghost" onClick={() => release(b)}>
                          Gỡ
                        </Button>
                      )}
                    </td>
                  </tr>
                )
              })}
              {assigned.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-slate-400">
                    Chưa có container nào được gán.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Modal>
      {picking && (
        <Modal title={`Gán container vào ${order.number}`} onClose={() => setPicking(false)}>
          <div className="mb-4 rounded-xl border border-cyan-100 bg-cyan-50 p-3 text-xs leading-5 text-cyan-800">
            Chỉ hiển thị container đang trong bãi, cùng hãng khai thác với lệnh,
            đúng loại và cỡ của một dòng còn chỗ, chưa gán cho lệnh khác.
          </div>
          <div className="space-y-2">
            {candidates.map((c) => (
              <label
                key={c.code}
                className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 p-3 text-sm"
              >
                <input
                  type="checkbox"
                  checked={selection.includes(c.code)}
                  disabled={!canTake(c)}
                  onChange={() => toggle(c.code)}
                />
                <span className="font-mono font-bold">{c.code}</span>
                <span className="text-slate-600">
                  {c.type} · {c.iso} · {c.size}
                </span>
                <span className="ml-auto font-mono text-xs">{c.location}</span>
              </label>
            ))}
            {candidates.length === 0 && (
              <div className="rounded-xl border border-slate-200 p-4 text-sm text-slate-500">
                Không có container phù hợp.
              </div>
            )}
          </div>
          <div className="mt-6 flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setPicking(false)}>
              Hủy
            </Button>
            <Button disabled={selection.length === 0} onClick={confirmAssign}>
              Gán {selection.length || ""} container
            </Button>
          </div>
        </Modal>
      )}
    </>
  )
}
function DeliveryOrders() {
  const orders = useStore().orders
  const setOrders = (next: DeliveryOrder[]) => setStore({ orders: next })
  const [detail, setDetail] = useState<string | null>(() => {
    const f = focusOrder
    focusOrder = null
    return f
  })
  const [filter, setFilter] = useState<string>(() => {
    const f = focusFilter
    focusFilter = null
    return f ?? ""
  })
  const isLive = (o: DeliveryOrder) =>
    ["Còn hạn", "Sắp hết hạn"].includes(orderStatus(o))
  const liveCount = orders.filter(isLive).length
  const expiringCount = orders.filter(
    (o) => orderStatus(o) === "Sắp hết hạn",
  ).length
  const shownOrders = orders.filter(
    (o) =>
      filter === "" ||
      (filter === "active" && isLive(o)) ||
      (filter === "expiring" && orderStatus(o) === "Sắp hết hạn") ||
      (filter.startsWith("tax:") && o.customer.startsWith(filter.slice(4))),
  )
  const filterLabel =
    filter === "expiring"
      ? "Lệnh sắp hết hạn (còn 3 ngày trở xuống)"
      : filter === "active"
        ? "Lệnh còn hiệu lực"
        : filter.startsWith("tax:")
          ? `Khách hàng: ${customers.find((c) => c.tax === filter.slice(4))?.name ?? filter.slice(4)}`
          : ""
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({
    number: "DO-2610-006",
    customer: "",
    operator: "",
    expiry: "",
    vessel: "",
  })
  const [lines, setLines] = useState<OrderLine[]>([
    { type: "Dry", size: "20ft", quantity: 1, delivered: 0 },
  ])
  const create = () => {
    if (
      !form.number ||
      !form.customer ||
      !form.operator ||
      !/^\d{2}\/\d{2}\/\d{4}$/.test(form.expiry) ||
      lines.some((l) => l.quantity < 1)
    )
      return
    setOrders([{ ...form, lines }, ...orders])
    setShowForm(false)
  }
  return (
    <>
      <PageHeader
        eyebrow="Kiểm soát giao nhận"
        title="Lệnh giao container"
        description="Hạn lệnh là ngày cuối container được phép giao."
        action={
          <Button icon="plus" onClick={() => setShowForm(true)}>
            Thêm lệnh giao
          </Button>
        }
      />
      <div className="mb-5 grid gap-4 sm:grid-cols-3">
        <button
          onClick={() => setFilter(filter === "active" ? "" : "active")}
          className={`rounded-2xl border bg-white p-4 text-left shadow-sm hover:bg-slate-50 ${
            filter === "active"
              ? "border-cyan-600 ring-2 ring-cyan-600/20"
              : "border-slate-200"
          }`}
        >
          <div className="text-sm text-slate-500">Lệnh còn hiệu lực</div>
          <div className="mt-2 text-2xl font-extrabold">{liveCount}</div>
        </button>
        <button
          onClick={() => setFilter(filter === "expiring" ? "" : "expiring")}
          className={`rounded-2xl border bg-amber-50 p-4 text-left hover:bg-amber-100/70 ${
            filter === "expiring"
              ? "border-amber-500 ring-2 ring-amber-500/30"
              : "border-amber-200"
          }`}
        >
          <div className="text-sm text-amber-700">Sắp hết hạn</div>
          <div className="mt-2 text-2xl font-extrabold text-amber-900">
            {expiringCount}
          </div>
          <div className="mt-1 text-xs font-semibold text-amber-700">
            Bấm để lọc danh sách
          </div>
        </button>
        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm text-slate-500">Đã giao hôm nay</div>
          <div className="mt-2 text-2xl font-extrabold">74</div>
        </div>
      </div>
      {filter && (
        <div className="mb-4 flex flex-wrap items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900">
          <span>
            Đang lọc: {filterLabel} · {shownOrders.length} lệnh
          </span>
          <button
            onClick={() => setFilter("")}
            className="ml-auto rounded-lg border border-amber-300 bg-white px-3 py-1 text-xs font-bold text-amber-800"
          >
            Bỏ lọc
          </button>
        </div>
      )}
      <Panel
        title="Danh sách lệnh giao"
        subtitle="Theo dõi hạn lệnh và tiến độ đã giao / tổng"
      >
        <TableTools placeholder="Tìm số lệnh, MST, khách hàng..." />
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <th>Số lệnh</th>
                <th>Khách hàng</th>
                <th>Hãng khai thác</th>
                <th>Hạn lệnh</th>
                <th>Chuyến tàu</th>
                <th>Số lượng theo loại</th>
                <th>Đã giao</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {shownOrders.map((order) => {
                const total = order.lines.reduce((s, l) => s + l.quantity, 0),
                  delivered = order.lines.reduce((s, l) => s + l.delivered, 0)
                return (
                  <tr
                    key={order.number}
                    onClick={() => setDetail(order.number)}
                    className="cursor-pointer border-t border-slate-100 hover:bg-slate-50/70"
                  >
                    <td className="font-mono font-bold">{order.number}</td>
                    <td className="max-w-60 font-medium">{order.customer}</td>
                    <td className="font-semibold">{order.operator}</td>
                    <td className="whitespace-nowrap">{order.expiry}</td>
                    <td className="whitespace-nowrap text-slate-500">
                      {order.vessel}
                    </td>
                    <td className="text-xs leading-5">
                      {order.lines
                        .map((l) => `${l.size} ${l.type} ×${l.quantity}`)
                        .join(" · ")}
                    </td>
                    <td>
                      <div className="min-w-24">
                        <div className="mb-1.5 flex justify-between text-xs">
                          <b>
                            {delivered} / {total}
                          </b>
                          <span>{Math.round((delivered / total) * 100)}%</span>
                        </div>
                        <div className="h-1.5 rounded-full bg-slate-100">
                          <div
                            className="h-full rounded-full bg-cyan-600"
                            style={{ width: `${(delivered / total) * 100}%` }}
                          />
                        </div>
                      </div>
                    </td>
                    <td>
                      <StatusBadge value={orderStatus(order)} />
                    </td>
                  </tr>
                )
              })}
              {shownOrders.length === 0 && (
                <tr>
                  <td colSpan={8} className="text-slate-400">
                    Không có lệnh giao phù hợp.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        <Pagination
          total={`${shownOrders.length} lệnh giao`}
          count={shownOrders.length}
        />
      </Panel>
      {detail && orderByNo(detail) && (
        <OrderDetail order={orderByNo(detail)!} onClose={() => setDetail(null)} />
      )}
      {showForm && (
        <Modal title="Thêm lệnh giao" onClose={() => setShowForm(false)} wide>
          <div className="grid gap-4 sm:grid-cols-2">
            <InputField
              label="Số lệnh *"
              value={form.number}
              onChange={(number) => setForm({ ...form, number })}
            />
            <SelectField
              label="Khách hàng *"
              value={form.customer}
              onChange={(customer) => setForm({ ...form, customer })}
              options={customers.map((c) => ({
                value: `${c.tax} – ${c.name}`,
                label: `${c.tax} – ${c.name}`,
              }))}
            />
            <SelectField
              label="Hãng khai thác *"
              value={form.operator}
              onChange={(operator) => setForm({ ...form, operator })}
              options={operators.map((o) => ({ value: o.name, label: o.name }))}
            />
            <InputField
              label="Hạn lệnh (ngày cuối được giao) *"
              value={form.expiry}
              onChange={(expiry) => setForm({ ...form, expiry })}
              placeholder="dd/MM/yyyy"
              icon="calendar"
            />
            <InputField
              label="Chuyến tàu xuất"
              value={form.vessel}
              onChange={(vessel) => setForm({ ...form, vessel })}
            />
          </div>
          <div className="mt-6">
            <div className="mb-3 flex items-center justify-between">
              <div className="text-sm font-bold">Số lượng theo loại</div>
              <Button
                variant="secondary"
                icon="plus"
                onClick={() =>
                  setLines([
                    ...lines,
                    { type: "Dry", size: "20ft", quantity: 1, delivered: 0 },
                  ])
                }
              >
                Thêm dòng
              </Button>
            </div>
            <div className="space-y-3">
              {lines.map((line, index) => (
                <div
                  key={index}
                  className="grid gap-3 rounded-xl border border-slate-200 p-3 sm:grid-cols-[1fr_120px_120px_auto]"
                >
                  <SelectField
                    label="Loại container"
                    value={line.type}
                    onChange={(type) =>
                      setLines(
                        lines.map((l, i) =>
                          i === index
                            ? { ...l, type: type as ContainerType }
                            : l,
                        ),
                      )
                    }
                    options={[
                      "Dry",
                      "Reefer",
                      "Open Top",
                      "Flat Rack",
                      "Bunker",
                      "Ventilated",
                      "Specialized",
                    ].map((v) => ({ value: v, label: v }))}
                  />
                  <SelectField
                    label="Cỡ"
                    value={line.size}
                    onChange={(size) =>
                      setLines(
                        lines.map((l, i) =>
                          i === index
                            ? { ...l, size: size as ContainerSize }
                            : l,
                        ),
                      )
                    }
                    options={["20ft", "40ft"].map((v) => ({
                      value: v,
                      label: v,
                    }))}
                  />
                  <InputField
                    label="Số lượng"
                    type="number"
                    value={line.quantity}
                    onChange={(quantity) =>
                      setLines(
                        lines.map((l, i) =>
                          i === index
                            ? { ...l, quantity: Number(quantity) }
                            : l,
                        ),
                      )
                    }
                  />
                  <div className="flex items-end">
                    <Button
                      variant="ghost"
                      disabled={lines.length === 1}
                      onClick={() =>
                        setLines(lines.filter((_, i) => i !== index))
                      }
                    >
                      Xóa dòng
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </div>
          <div className="mt-6 flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setShowForm(false)}>
              Hủy
            </Button>
            <Button onClick={create}>Tạo lệnh giao</Button>
          </div>
        </Modal>
      )}
    </>
  )
}

const operatorTones: Record<string, string> = {
  Maersk: "cyan",
  Evergreen: "emerald",
  "CMA CGM": "rose",
  OOCL: "violet",
  "Hapag-Lloyd": "amber",
  "Ocean Network Express": "indigo",
}
const toneClasses: Record<string, string> = {
  cyan: "border-cyan-700 bg-cyan-600 text-white",
  indigo: "border-indigo-700 bg-indigo-600 text-white",
  emerald: "border-emerald-700 bg-emerald-600 text-white",
  amber: "border-amber-700 bg-amber-500 text-white",
  violet: "border-violet-700 bg-violet-600 text-white",
  rose: "border-rose-700 bg-rose-600 text-white",
  slate: "border-slate-700 bg-slate-600 text-white",
  pale: "border-slate-300 bg-slate-200 text-slate-600",
  empty: "border-dashed border-slate-300 bg-slate-50 text-slate-400",
}
function Yard() {
  const [selectedBlock, setSelectedBlock] =
    useState<"A" | "B" | "C" | "D" | "VB-01">("A")
  const [tier, setTier] = useState(1)
  const [colorMode, setColorMode] = useState<"size" | "type" | "operator" | "booking">(
    "size",
  )
  const [configs, setConfigs] = useState(defaultBlocks)
  const [selectedCode, setSelectedCode] = useState("EGHU 6034211")
  const [notice, setNotice] = useState("")
  useStore()
  const config = selectedBlock === "VB-01" ? null : configs[selectedBlock]
  const selectedContainer = initialContainers.find(
    (c) => c.code === selectedCode,
  )
  const selectedPlacement = placements.find((p) => p.code === selectedCode)
  const blockPlacements =
    selectedBlock === "VB-01"
      ? []
      : placements.filter((p) => p.block === selectedBlock && p.tier === tier)
  const toneFor = (c: Container) =>
    colorMode === "booking"
      ? bookingTone(c)
      : colorMode === "size"
      ? c.type === "Reefer"
        ? "emerald"
        : c.size === "40ft"
          ? "indigo"
          : "cyan"
      : colorMode === "type"
        ? {
            Dry: "cyan",
            Reefer: "emerald",
            "Open Top": "amber",
            "Flat Rack": "violet",
            Bunker: "rose",
            Ventilated: "indigo",
            Specialized: "slate",
          }[c.type]
        : operatorTones[c.operator] || "slate"
  const legends = useMemo(() => {
    if (colorMode === "size")
      return [
        ["cyan", "20ft"],
        ["indigo", "40ft"],
        ["emerald", "Reefer"],
      ]
    if (colorMode === "type")
      return [
        ["cyan", "Dry"],
        ["emerald", "Reefer"],
        ["amber", "Open Top"],
        ["violet", "Flat Rack"],
        ["rose", "Bunker"],
        ["indigo", "Ventilated"],
        ["slate", "Specialized"],
      ]
    if (colorMode === "booking")
      return [
        ["cyan", "Đã giữ chỗ"],
        ["pale", "Chưa gán"],
        ["amber", "Sắp hết hạn"],
      ]
    const names = [
      ...new Set(
        blockPlacements
          .map(
            (p) => initialContainers.find((c) => c.code === p.code)?.operator,
          )
          .filter(Boolean),
      ),
    ] as string[]
    return names.map((name) => [operatorTones[name] || "slate", name])
  }, [colorMode, blockPlacements])
  const updateConfig = (key: keyof BlockConfig, delta: number) => {
    if (!config || selectedBlock === "VB-01") return
    const next = config[key] + delta
    if (next < 1) return
    const blockedPlacement = placements.find(
      (p) =>
        p.block === selectedBlock &&
        ((key === "slots" &&
          p.slot +
            (initialContainers.find((c) => c.code === p.code)?.size === "40ft"
              ? 1
              : 0) >
            next) ||
          (key === "rows" && p.row > next) ||
          (key === "tiers" && p.tier > next)),
    )
    if (blockedPlacement) {
      setNotice(
        `Không thể thu hẹp: container ${blockedPlacement.code} tại ${formatPosition(blockedPlacement)} nằm trong vùng bị cắt.`,
      )
      return
    }
    setNotice("")
    setConfigs({ ...configs, [selectedBlock]: { ...config, [key]: next } })
    if (key === "tiers" && tier > next) setTier(next)
  }
  const occupancy = (block: "A" | "B" | "C" | "D") =>
    Math.round(
      (placements
        .filter((p) => p.block === block)
        .reduce(
          (sum, p) =>
            sum +
            (initialContainers.find((c) => c.code === p.code)?.size === "40ft"
              ? 2
              : 1),
          0,
        ) /
        (configs[block].slots * configs[block].rows * configs[block].tiers)) *
        100,
    )
  const virtual = initialContainers.filter((c) => c.location === "VB-01")
  return (
    <>
      <PageHeader
        eyebrow="Khai thác bãi"
        title="Sơ đồ vị trí bãi"
        description="Theo dõi container theo đúng Block, Bay, Row và Tier."
        action={
          <div className="flex gap-2">
            <Button variant="secondary">2D</Button>
            <Button variant="secondary">Danh sách</Button>
          </div>
        }
      />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        {(["A", "B", "C", "D"] as const).map((block) => (
          <button
            key={block}
            onClick={() => {
              setSelectedBlock(block)
              setTier(1)
              setNotice("")
            }}
            className={`rounded-2xl border bg-white p-4 text-left shadow-sm transition hover:-translate-y-0.5 ${
              selectedBlock === block
                ? "border-cyan-500 ring-2 ring-cyan-100"
                : "border-slate-200"
            }`}
          >
            <div className="flex items-center justify-between">
              <span className="font-bold">Block {block}</span>
              <span className="text-sm font-bold text-cyan-700">
                {occupancy(block)}%
              </span>
            </div>
            <div className="mt-1 text-xs text-slate-400">
              {configs[block].slots} bay lẻ · {configs[block].rows} Row ·{" "}
              {configs[block].tiers} Tier
            </div>
            <div className="mt-3 h-2 rounded-full bg-slate-100">
              <div
                className="h-full rounded-full bg-cyan-600"
                style={{ width: `${occupancy(block)}%` }}
              />
            </div>
          </button>
        ))}
        <button
          onClick={() => setSelectedBlock("VB-01")}
          className={`rounded-2xl border border-dashed bg-violet-50/60 p-4 text-left ${
            selectedBlock === "VB-01"
              ? "border-violet-500 ring-2 ring-violet-100"
              : "border-violet-300"
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="font-bold text-violet-900">Block ảo VB-01</span>
            <span className="rounded-full bg-violet-100 px-2 py-1 text-xs font-bold text-violet-700">
              {virtual.length} cont
            </span>
          </div>
          <div className="mt-1 text-xs text-violet-600">
            Không quản lý Bay, Row, Tier
          </div>
        </button>
      </div>
      {selectedBlock === "VB-01" ? (
        <Panel
          className="mt-5"
          title="Container trong Block ảo VB-01"
          subtitle="Danh sách container chờ phân vị trí"
        >
          <VirtualTable items={virtual} />
        </Panel>
      ) : (
        config && (
          <>
            <Panel
              className="mt-5"
              title={`Block ${selectedBlock} — Mặt bằng Bay`}
              subtitle={`${config.slots} bay lẻ (slot 20ft) × ${config.rows} Row × ${config.tiers} Tier`}
              action={
                <div className="hidden rounded-lg bg-slate-100 p-1 md:flex">
                  {[
                    ["size", "Kích thước"],
                    ["type", "Loại cont"],
                    ["operator", "Hãng khai thác"],
                    ["booking", "Theo đặt chỗ"],
                  ].map(([id, label]) => (
                    <button
                      key={id}
                      onClick={() => setColorMode(id as typeof colorMode)}
                      className={`rounded-md px-3 py-1.5 text-xs font-bold ${
                        colorMode === id
                          ? "bg-white shadow-sm"
                          : "text-slate-500"
                      }`}
                    >
                      {label}
                    </button>
                  ))}
                </div>
              }
            >
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 bg-slate-50/70 px-5 py-3">
                <div className="flex flex-wrap gap-4 text-xs font-semibold text-slate-600">
                  {legends.map(([tone, label]) => (
                    <span key={label} className="flex items-center gap-2">
                      <i
                        className={`h-3 w-5 rounded-sm ${toneClasses[tone].split(" ").find((c) => c.startsWith("bg-"))}`}
                      />
                      {label}
                    </span>
                  ))}
                  <span className="flex items-center gap-2">
                    <i className="h-3 w-5 rounded-sm border border-dashed border-slate-300 bg-white" />
                    Còn trống
                  </span>
                </div>
                <span className="text-xs text-slate-400">
                  Cập nhật lúc 09:42
                </span>
              </div>
              <div className="border-b border-slate-100 px-5 py-3">
                <div className="flex flex-wrap gap-2">
                  {Array.from({ length: config.tiers }, (_, i) => i + 1).map(
                    (value) => (
                      <button
                        key={value}
                        onClick={() => setTier(value)}
                        className={`rounded-lg px-3 py-2 text-xs font-bold ${
                          tier === value
                            ? "bg-slate-900 text-white"
                            : "border border-slate-200 text-slate-600"
                        }`}
                      >
                        Tier {value}
                      </button>
                    ),
                  )}
                </div>
              </div>
              <div className="grid xl:grid-cols-[1fr_310px]">
                <div className="overflow-x-auto p-5">
                  <div
                    style={{
                      minWidth: `${Math.max(720, config.slots * 105)}px`,
                    }}
                  >
                    <div
                      className="mb-2 grid gap-1.5 text-center"
                      style={{
                        gridTemplateColumns: `42px repeat(${config.slots}, minmax(88px, 1fr))`,
                      }}
                    >
                      <span />
                      {Array.from({ length: config.slots }, (_, i) => (
                        <span
                          key={i}
                          className="rounded-md bg-cyan-50 py-1.5 text-[11px] font-extrabold text-cyan-800"
                        >
                          Bay {String(i * 2 + 1).padStart(2, "0")}
                        </span>
                      ))}
                    </div>
                    <div className="mb-3 ml-12 rounded-lg border border-indigo-100 bg-indigo-50/70 px-3 py-2 text-center text-[11px] font-semibold text-indigo-700">
                      Bay chẵn nằm giữa hai Bay lẻ liền kề và dành cho container
                      40ft
                    </div>
                    <div className="space-y-2">
                      {Array.from(
                        { length: config.rows },
                        (_, rowIndex) => rowIndex + 1,
                      ).map((row) => (
                        <div
                          key={row}
                          className="grid gap-1.5"
                          style={{
                            gridTemplateColumns: `42px repeat(${config.slots}, minmax(88px, 1fr))`,
                          }}
                        >
                          <div className="flex items-center justify-center rounded-lg bg-slate-900 text-xs font-extrabold text-white">
                            {String.fromCharCode(64 + row)}
                          </div>
                          {Array.from(
                            { length: config.slots },
                            (_, slotIndex) => slotIndex + 1,
                          ).map((slot) => {
                            const p = blockPlacements.find(
                              (item) => item.row === row && item.slot === slot,
                            )
                            const covered = blockPlacements.some((item) => {
                              const c = initialContainers.find(
                                (x) => x.code === item.code,
                              )
                              return (
                                item.row === row &&
                                c?.size === "40ft" &&
                                item.slot + 1 === slot
                              )
                            })
                            if (covered) return null
                            const c = p
                              ? initialContainers.find((x) => x.code === p.code)
                              : null
                            const span = c?.size === "40ft" ? 2 : 1
                            const bay =
                              c?.size === "40ft" ? slot * 2 : slot * 2 - 1
                            return (
                              <button
                                key={slot}
                                onClick={() => c && setSelectedCode(c.code)}
                                style={{
                                  gridColumn: `${slot + 1} / span ${span}`,
                                }}
                                className={`relative h-16 overflow-hidden rounded-lg border px-2 text-left shadow-sm ${
                                  c
                                    ? toneClasses[toneFor(c)]
                                    : toneClasses.empty
                                }`}
                                title={
                                  c
                                    ? `${c.code} · Bay ${String(bay).padStart(2, "0")} · Row ${String.fromCharCode(64 + row)} · Tier ${tier}`
                                    : "Vị trí trống"
                                }
                              >
                                {c ? (
                                  <>
                                    <span className="block truncate font-mono text-[10px] font-extrabold">
                                      {c.code}
                                    </span>
                                    <span className="mt-1 flex justify-between text-[10px] font-bold">
                                      <span>{c.size}</span>
                                      <span>
                                        Bay {String(bay).padStart(2, "0")}
                                      </span>
                                    </span>
                                  </>
                                ) : (
                                  <span className="block text-center text-[10px] font-bold">
                                    Trống
                                  </span>
                                )}
                              </button>
                            )
                          })}
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
                <aside className="border-t border-slate-100 bg-slate-50/60 p-5 xl:border-l xl:border-t-0">
                  <div className="text-[10px] font-bold uppercase tracking-[0.16em] text-cyan-700">
                    Vị trí đang chọn
                  </div>
                  {selectedContainer &&
                  selectedPlacement?.block === selectedBlock &&
                  selectedPlacement.tier === tier ? (
                    <>
                      <div className="mt-2 font-mono text-lg font-extrabold">
                        {formatPosition(selectedPlacement)}
                      </div>
                      <div className="mt-4 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
                        <div className="flex items-center justify-between">
                          <span className="font-mono text-sm font-extrabold">
                            {selectedContainer.code}
                          </span>
                          <StatusBadge value={selectedContainer.status} />
                        </div>
                        <dl className="mt-4 space-y-2 text-sm">
                          {[
                            ["Loại", selectedContainer.type],
                            ["Mã ISO", selectedContainer.iso],
                            ["Kích thước", selectedContainer.size],
                            ["Hãng khai thác", selectedContainer.operator],
                            ["Phân loại", selectedContainer.classification],
                            ["Tình trạng", selectedContainer.condition],
                            [
                              "Trọng lượng tối đa",
                              `${selectedContainer.maxWeight.toLocaleString("vi-VN")} kg`,
                            ],
                            [
                              "Trọng lượng vỏ",
                              `${selectedContainer.tareWeight.toLocaleString("vi-VN")} kg`,
                            ],
                            ["Ngày sản xuất", selectedContainer.manufactured],
                            ["Vị trí", formatPosition(selectedPlacement)],
                            ["Lệnh giao", yardOrderText(selectedContainer.code)],
                          ].map(([label, value]) => (
                            <div
                              key={label}
                              className="flex justify-between gap-3"
                            >
                              <dt className="text-slate-500">{label}</dt>
                              <dd className="text-right font-bold">{value}</dd>
                            </div>
                          ))}
                        </dl>
                        {selectedContainer.size === "40ft" && (
                          <div className="mt-3 rounded-lg bg-indigo-50 p-2 text-xs font-semibold text-indigo-700">
                            Bay chẵn{" "}
                            {String(selectedPlacement.slot * 2).padStart(
                              2,
                              "0",
                            )}{" "}
                            (chiếm bay lẻ{" "}
                            {String(selectedPlacement.slot * 2 - 1).padStart(
                              2,
                              "0",
                            )}{" "}
                            và{" "}
                            {String(selectedPlacement.slot * 2 + 1).padStart(
                              2,
                              "0",
                            )}
                            )
                          </div>
                        )}
                      </div>
                    </>
                  ) : (
                    <div className="mt-3 text-sm text-slate-400">
                      Chọn một container tại Tier đang xem.
                    </div>
                  )}
                </aside>
              </div>
            </Panel>
            <div className="mt-5 grid gap-5 lg:grid-cols-[1.2fr_1fr]">
              <Panel
                title="Quy tắc xếp bãi"
                subtitle="Áp dụng khi kiểm tra vị trí"
              >
                <div className="grid gap-3 p-5 sm:grid-cols-3">
                  {[
                    [
                      "Bay chẵn / lẻ",
                      "Bay lẻ chứa 20ft. Bay chẵn chứa 40ft và chiếm hai bay lẻ liền kề.",
                    ],
                    [
                      "Đỡ tải tầng trên",
                      "Tầng trên phải được tầng dưới đỡ toàn bộ chiều dài.",
                    ],
                    [
                      "Tương thích kích thước",
                      "40ft xếp trên hai 20ft hoặc một 40ft; 20ft không xếp trên 40ft.",
                    ],
                  ].map(([title, text], i) => (
                    <div
                      key={title}
                      className="rounded-xl border border-slate-200 bg-slate-50 p-4"
                    >
                      <span className="grid size-7 place-items-center rounded-full bg-slate-900 text-xs font-bold text-white">
                        {i + 1}
                      </span>
                      <div className="mt-3 text-sm font-bold">{title}</div>
                      <p className="mt-1 text-xs leading-5 text-slate-500">
                        {text}
                      </p>
                    </div>
                  ))}
                </div>
              </Panel>
              <Panel
                title={`Cấu hình Block ${selectedBlock}`}
                subtitle="Sơ đồ cập nhật ngay khi thay đổi"
              >
                <div className="space-y-3 p-5">
                  {[
                    ["Số bay lẻ (slot 20ft)", "slots"],
                    ["Số Row", "rows"],
                    ["Tier tối đa", "tiers"],
                  ].map(([label, key]) => (
                    <div
                      key={key}
                      className="flex items-center justify-between rounded-xl border border-slate-200 p-3"
                    >
                      <span className="text-sm font-semibold">{label}</span>
                      <div className="flex items-center gap-3">
                        <button
                          onClick={() =>
                            updateConfig(key as keyof BlockConfig, -1)
                          }
                          className="grid size-7 place-items-center rounded-md bg-slate-100 font-bold"
                        >
                          −
                        </button>
                        <span className="w-6 text-center text-sm font-extrabold">
                          {config[(key as keyof BlockConfig)]}
                        </span>
                        <button
                          onClick={() =>
                            updateConfig(key as keyof BlockConfig, 1)
                          }
                          className="grid size-7 place-items-center rounded-md bg-cyan-50 font-bold text-cyan-700"
                        >
                          +
                        </button>
                      </div>
                    </div>
                  ))}
                  {notice && (
                    <div className="rounded-xl border border-rose-200 bg-rose-50 p-3 text-xs leading-5 text-rose-700">
                      {notice}
                    </div>
                  )}
                </div>
              </Panel>
            </div>
          </>
        )
      )}
    </>
  )
}
function VirtualTable({ items }: { items: Container[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
            <th>Số container</th>
            <th>Loại · ISO</th>
            <th>Kích thước</th>
            <th>Hãng khai thác</th>
            <th>Ghi chú</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.code} className="border-t border-slate-100">
              <td className="font-mono font-bold">{item.code}</td>
              <td>
                {item.type} · {item.iso}
              </td>
              <td>{item.size}</td>
              <td>{item.operator}</td>
              <td className="text-slate-500">{item.status}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function Customers() {
  const [showForm, setShowForm] = useState(false)
  useStore()
  return (
    <>
      <PageHeader
        eyebrow="Đối tác · Mở rộng"
        title="Khách hàng & hãng khai thác"
        description="Hai danh mục độc lập phục vụ đối chiếu lệnh giao và container."
        action={
          <Button icon="plus" onClick={() => setShowForm(true)}>
            Thêm khách hàng
          </Button>
        }
      />
      <div className="grid gap-5 xl:grid-cols-2">
        <Panel
          title="Hãng khai thác (line operator)"
          subtitle={`${operators.length} hãng trong dữ liệu mẫu`}
        >
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="bg-slate-50 text-xs uppercase text-slate-500">
                  <th>Hãng khai thác</th>
                  <th>Mã</th>
                  <th>Liên hệ</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {operators.map((item) => (
                  <tr key={item.code} className="border-t border-slate-100">
                    <td className="font-bold">{item.name}</td>
                    <td className="font-mono">{item.code}</td>
                    <td className="text-slate-500">{item.contact}</td>
                    <td>
                      <StatusBadge value={item.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Panel>
        <Panel
          title="Khách hàng"
          subtitle="Doanh nghiệp nhận container theo lệnh giao"
        >
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="bg-slate-50 text-xs uppercase text-slate-500">
                  <th>MST</th>
                  <th>Tên khách hàng</th>
                  <th>Liên hệ</th>
                  <th>Trạng thái</th>
                  <th>Lệnh giao</th>
                </tr>
              </thead>
              <tbody>
                {customers.map((item) => (
                  <tr key={item.tax} className="border-t border-slate-100">
                    <td className="font-mono font-bold">{item.tax}</td>
                    <td className="font-bold">{item.name}</td>
                    <td className="text-slate-500">{item.contact}</td>
                    <td>
                      <StatusBadge value={item.status} />
                    </td>
                    <td>
                      {(() => {
                        const mine = store.orders.filter((o) =>
                          o.customer.startsWith(item.tax),
                        )
                        const live = mine.filter((o) =>
                          ["Còn hạn", "Sắp hết hạn"].includes(orderStatus(o)),
                        ).length
                        return (
                          <button
                            disabled={mine.length === 0}
                            onClick={() =>
                              navigateTo(
                                "deliveryOrders",
                                undefined,
                                `tax:${item.tax}`,
                              )
                            }
                            className="text-left font-bold text-sky-700 hover:underline disabled:text-slate-400 disabled:no-underline"
                          >
                            {mine.length} DO
                            <span className="block text-xs font-medium text-slate-400">
                              {live} còn hiệu lực
                            </span>
                          </button>
                        )
                      })()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Panel>
      </div>
      {showForm && (
        <Modal title="Thêm khách hàng" onClose={() => setShowForm(false)}>
          <div className="grid gap-4 sm:grid-cols-2">
            <InputField label="Mã số thuế *" placeholder="Nhập mã số thuế" />
            <InputField
              label="Tên khách hàng *"
              placeholder="Nhập tên doanh nghiệp"
            />
            <InputField label="Email liên hệ" placeholder="email@company.com" />
            <InputField
              label="Số điện thoại"
              placeholder="Nhập số điện thoại"
            />
            <InputField label="Địa chỉ" placeholder="Nhập địa chỉ" />
          </div>
          <div className="mt-6 flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setShowForm(false)}>
              Hủy
            </Button>
            <Button onClick={() => setShowForm(false)}>Thêm khách hàng</Button>
          </div>
        </Modal>
      )}
    </>
  )
}
function Reports() {
  return (
    <>
      <PageHeader
        eyebrow="Phân tích vận hành · Mở rộng"
        title="Báo cáo"
        description="Theo dõi sản lượng và hiệu suất khai thác depot."
        action={<Button icon="download">Xuất báo cáo</Button>}
      />
      <Panel
        title="Bộ lọc báo cáo"
        subtitle="Chọn loại báo cáo và khoảng thời gian"
      >
        <div className="grid gap-4 p-5 md:grid-cols-4">
          <InputField
            label="Loại báo cáo"
            placeholder="Sản lượng nhập / xuất"
          />
          <InputField label="Từ ngày" value="01/10/2026" icon="calendar" />
          <InputField label="Đến ngày" value="12/10/2026" icon="calendar" />
          <div className="flex items-end">
            <Button>Xem báo cáo</Button>
          </div>
        </div>
      </Panel>
      <div className="mt-5 grid gap-4 sm:grid-cols-3">
        <MetricCard
          label="Tổng lượt nhập"
          value="1.486"
          trend="12,5%"
          icon="arrowDown"
          tone="cyan"
        />
        <MetricCard
          label="Tổng lượt xuất"
          value="1.328"
          trend="9,2%"
          icon="arrowUp"
          tone="violet"
        />
        <MetricCard
          label="Thời gian lưu bãi TB"
          value="6,4 ngày"
          trend="3,1%"
          icon="calendar"
          tone="emerald"
        />
      </div>
      <Panel
        className="mt-5"
        title="Sản lượng theo hãng khai thác"
        subtitle="Từ 01/10/2026 đến 12/10/2026"
      >
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="bg-slate-50 text-xs uppercase text-slate-500">
                <th>Hãng khai thác</th>
                <th>Nhập bãi</th>
                <th>Xuất bãi</th>
                <th>Đang lưu bãi</th>
                <th>Tổng lượt</th>
              </tr>
            </thead>
            <tbody>
              {operators.slice(0, 4).map((item, i) => (
                <tr key={item.code} className="border-t border-slate-100">
                  <td className="font-bold">{item.name}</td>
                  <td>{[342, 286, 215, 174][i]}</td>
                  <td>{[318, 251, 208, 151][i]}</td>
                  <td>{item.containers}</td>
                  <td className="font-bold">{[660, 537, 423, 325][i]}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Panel>
    </>
  )
}
function Login({ onLogin }: { onLogin: () => void }) {
  return (
    <div className="grid min-h-screen bg-slate-950 lg:grid-cols-2">
      <div className="relative hidden overflow-hidden p-12 lg:flex lg:flex-col lg:justify-between">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,rgba(8,145,178,.28),transparent_40%),radial-gradient(circle_at_80%_80%,rgba(99,102,241,.18),transparent_40%)]" />
        <Logo light />
        <div className="relative max-w-lg">
          <div className="mb-5 h-1 w-14 rounded-full bg-cyan-500" />
          <h1 className="text-4xl font-bold leading-tight text-white">
            Vận hành depot
            <br />
            chính xác, liền mạch.
          </h1>
          <p className="mt-5 leading-7 text-slate-400">
            Một nền tảng duy nhất để quản lý container, cổng ra vào, vị trí bãi
            và hiệu suất khai thác.
          </p>
        </div>
        <p className="relative text-xs text-slate-600">
          © 2026 SNP Depot Management
        </p>
      </div>
      <div className="flex items-center justify-center bg-slate-50 p-6">
        <div className="w-full max-w-md">
          <div className="rounded-2xl border border-slate-200 bg-white p-9 shadow-xl">
            <div className="mb-7">
              <div className="text-xs font-bold uppercase tracking-[0.16em] text-cyan-700">
                Đăng nhập hệ thống · Mở rộng
              </div>
              <h2 className="mt-2 text-2xl font-bold">Chào mừng trở lại</h2>
            </div>
            <div className="space-y-4">
              <InputField
                label="Tên đăng nhập"
                placeholder="Nhập tên đăng nhập"
              />
              <InputField
                label="Mật khẩu"
                type="password"
                placeholder="Nhập mật khẩu"
              />
            </div>
            <button
              onClick={onLogin}
              className="mt-6 w-full rounded-lg bg-cyan-700 py-3 text-sm font-bold text-white"
            >
              Đăng nhập
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
function Logo({ light = false }: { light?: boolean }) {
  return (
    <div className="relative flex items-center gap-3">
      <div className="grid size-10 place-items-center rounded-xl bg-cyan-600 text-white shadow-lg">
        <Icon name="box" size={23} />
      </div>
      <div>
        <div
          className={`text-base font-extrabold ${
            light ? "text-white" : "text-slate-900"
          }`}
        >
          SNP Depot
        </div>
        <div
          className={`text-[10px] font-semibold uppercase tracking-[0.14em] ${
            light ? "text-slate-500" : "text-slate-400"
          }`}
        >
          Management
        </div>
      </div>
    </div>
  )
}

export default function App() {
  const [page, setPage] = useState<Page>("dashboard")
  const [loggedIn, setLoggedIn] = useState(true)
  const [mobileOpen, setMobileOpen] = useState(false)
  const activeLabel = useMemo(
    () => navItems.find((item) => item.id === page)?.label,
    [page],
  )
  if (!loggedIn) return <Login onLogin={() => setLoggedIn(true)} />
  const navigate = (next: Page) => {
    setPage(next)
    setMobileOpen(false)
  }
  navigateTo = (next: Page, order?: string, filter?: string) => {
    focusOrder = order ?? null
    focusFilter = filter ?? null
    navigate(next)
  }
  const screens: Record<Page, ReactNode> = {
    dashboard: <Dashboard navigate={navigate} />,
    containers: <Containers />,
    gate: <Gate />,
    deliveryOrders: <DeliveryOrders />,
    yard: <Yard />,
    customers: <Customers />,
    reports: <Reports />,
  }
  return (
    <div className="min-h-screen bg-slate-50 text-slate-800">
      {mobileOpen && (
        <button
          aria-label="Đóng menu"
          onClick={() => setMobileOpen(false)}
          className="fixed inset-0 z-30 bg-slate-950/30 lg:hidden"
        />
      )}
      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-64 flex-col border-r border-slate-200 bg-white transition-transform lg:translate-x-0 ${
          mobileOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="flex h-20 items-center justify-between border-b border-slate-100 px-6">
          <Logo />
          <button
            onClick={() => setMobileOpen(false)}
            className="text-slate-400 lg:hidden"
          >
            <Icon name="close" />
          </button>
        </div>
        <nav className="flex-1 space-y-1 p-4">
          <div className="mb-3 px-3 text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
            Điều hành
          </div>
          {navItems.map((item) => (
            <button
              key={item.id}
              onClick={() => navigate(item.id)}
              className={`group flex w-full items-center gap-3 rounded-xl px-3 py-3 text-sm font-semibold ${
                page === item.id
                  ? "bg-cyan-50 text-cyan-800"
                  : "text-slate-500 hover:bg-slate-50"
              }`}
            >
              <span
                className={
                  page === item.id ? "text-cyan-700" : "text-slate-400"
                }
              >
                <Icon name={item.icon} />
              </span>
              {item.label}
              {item.extended && (
                <span className="ml-auto rounded-full bg-slate-100 px-2 py-0.5 text-[9px] font-bold text-slate-500">
                  Mở rộng
                </span>
              )}
              {page === item.id && !item.extended && (
                <span className="ml-auto size-1.5 rounded-full bg-cyan-600" />
              )}
            </button>
          ))}
        </nav>
        <div className="border-t border-slate-100 p-4">
          <div className="rounded-xl bg-slate-50 p-3">
            <div className="flex items-center gap-3">
              <div className="grid size-9 place-items-center rounded-full bg-slate-900 text-xs font-bold text-white">
                NM
              </div>
              <div className="min-w-0 flex-1">
                <div className="truncate text-sm font-bold">Nguyễn Minh</div>
                <div className="text-xs text-slate-500">Điều phối viên</div>
              </div>
              <button
                onClick={() => setLoggedIn(false)}
                title="Đăng xuất"
                className="text-slate-400"
              >
                <Icon name="logout" size={18} />
              </button>
            </div>
          </div>
        </div>
      </aside>
      <div className="lg:pl-64">
        <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-slate-200 bg-white/90 px-4 backdrop-blur md:px-7">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setMobileOpen(true)}
              className="rounded-lg p-2 text-slate-500 lg:hidden"
            >
              <Icon name="menu" />
            </button>
            <div className="hidden items-center gap-2 text-sm sm:flex">
              <span className="text-slate-400">SNP Depot</span>
              <Icon name="chevron" size={15} />
              <span className="font-semibold">{activeLabel}</span>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <span className="hidden rounded-full bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-700 sm:inline-flex">
              Hệ thống ổn định
            </span>
            <button className="relative rounded-lg p-2.5 text-slate-500">
              <Icon name="bell" />
              <span className="absolute right-2 top-2 size-2 rounded-full bg-rose-500 ring-2 ring-white" />
            </button>
          </div>
        </header>
        <main className="mx-auto max-w-[1500px] p-4 md:p-7 xl:p-8">
          {screens[page]}
        </main>
      </div>
    </div>
  )
}
