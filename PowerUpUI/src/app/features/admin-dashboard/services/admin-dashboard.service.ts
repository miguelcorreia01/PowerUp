import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AdminDashboardSummary {
  totalUsers: number;
  totalMembers: number;
  totalInstructors: number;
  newUsersLast30Days: number;
  activeSubscriptions: number;
  monthlyRevenue: number;
  topMembership: string | null;
  topMembershipSubscriptions: number;
  upcomingGroupClasses: number;
  personalTrainingSessionsNext7Days: number;
}

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5255/api/admin/dashboard';

  getOverview(): Observable<AdminDashboardSummary> {
    return this.http.get<AdminDashboardSummary>(`${this.baseUrl}/overview`);
  }
}