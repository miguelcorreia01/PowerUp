import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, switchMap } from 'rxjs';

type Subscription = {
  id: string;
  type: 'Monthly' | 'Semestral' | 'Yearly';
  totalPrice: number;
};

type UserSubscription = {
  id: string;
  userId: string;
  subscriptionId: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
};

type GroupClass = {
  id: string;
  name: string;
  description: string;
  startTime: string;
  maxCapacity: number;
  currentEnrollment: number;
  members: Member[];
};

type Member = {
  id: string;
  userId: string;
};

type PtSession = {
  id: string;
  instructorId: string;
  memberId: string;
  price: number;
  sessionTime: string;
  status: 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow';
};

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5255/api';

  getMyUserSubscription() {
    return this.http.get<UserSubscription>(`${this.baseUrl}/subscriptions/my`);
  }

  getSubscriptionById(subscriptionId: string) {
    return this.http.get<Subscription>(`${this.baseUrl}/subscriptions/${subscriptionId}`);
  }

  getMembership() {
    return this.getMyUserSubscription().pipe(
      switchMap(us =>
        this.getSubscriptionById(us.subscriptionId).pipe(
          map(sub => {
            const end = new Date(us.endDate);
            const today = new Date();
            const ms = end.getTime() - today.setHours(0, 0, 0, 0);
            const expiresIn = Math.max(0, Math.ceil(ms / (1000 * 60 * 60 * 24)));
            const progress = this.computeProgress(us.startDate, us.endDate);
            return {
              type: sub.type,
              expiresIn,
              progress
            } as { type: string; expiresIn: number; progress: number };
          })
        )
      )
    );
  }

  getAllGroupClasses() {
    return this.http.get<GroupClass[]>(`${this.baseUrl}/groupclass`);
  }

  // Get classes where the user is enrolled
  getEnrolledGroupClasses(userId: string) {
    return this.getAllGroupClasses().pipe(
      map(classes => {
        return classes.filter(classItem => {
          return classItem.members?.some(member => 
            member.userId?.toLowerCase() === userId.toLowerCase()
          );
        });
      })
    );
  }

  // Get today's enrolled classes for the user
  getTodayEnrolledClasses(userId: string) {
    return this.getEnrolledGroupClasses(userId).pipe(
      map(classes => {
        const today = new Date();
        return classes
          .filter(classItem => {
            const classDate = new Date(classItem.startTime);
            return isSameDay(classDate, today);
          })
          .sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime())
          .map(classItem => ({
            time: formatHM(classItem.startTime),
            name: classItem.name,
            instructor: '',
            spots: classItem.currentEnrollment,
            maxSpots: classItem.maxCapacity
          }));
      })
    );
  }

  // Get group classes summary for dashboard cards
  getGroupClassesSummary(userId: string) {
    return this.getEnrolledGroupClasses(userId).pipe(
      map(classes => {
        const today = new Date();
        const todayClasses = classes.filter(classItem => {
          const classDate = new Date(classItem.startTime);
          return isSameDay(classDate, today);
        });
        
        const nextClass = classes
          .filter(classItem => new Date(classItem.startTime) > today)
          .sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime())[0];
        
        return {
          enrolled: todayClasses.length,
          nextClass: nextClass ? formatTime(nextClass.startTime) : 'No upcoming classes'
        };
      })
    );
  }

  getAllPtSessions() {
    return this.http.get<PtSession[]>(`${this.baseUrl}/ptsession`);
  }

  // Get personal training summary for dashboard cards
  getPersonalTrainingSummary(userId: string) {
    return this.getAllPtSessions().pipe(
      map(sessions => {
        const userSessions = sessions.filter(session => 
          session.memberId?.toLowerCase() === userId.toLowerCase()
        );
        
        const today = new Date();
        const todaySessions = userSessions.filter(session => {
          const sessionDate = new Date(session.sessionTime);
          return isSameDay(sessionDate, today);
        });
        
        const nextSession = userSessions
          .filter(session => new Date(session.sessionTime) > today)
          .sort((a, b) => new Date(a.sessionTime).getTime() - new Date(b.sessionTime).getTime())[0];
        
        return {
          booked: todaySessions.length,
          nextSession: nextSession ? formatTime(nextSession.sessionTime) : 'No upcoming sessions'
        };
      })
    );
  }

  private computeProgress(startDate: string, endDate: string): number {
    const start = new Date(startDate).getTime();
    const end = new Date(endDate).getTime();
    const now = Date.now();
    if (now <= start) return 0;
    if (now >= end) return 100;
    return Math.round(((now - start) / (end - start)) * 100);
  }
}


function isSameDay(a: Date, b: Date) {
  return a.getFullYear() === b.getFullYear() && 
         a.getMonth() === b.getMonth() && 
         a.getDate() === b.getDate();
}

function formatHM(dateString: string) {
  const date = new Date(dateString);
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function formatTime(dateString: string) {
  const date = new Date(dateString);
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}