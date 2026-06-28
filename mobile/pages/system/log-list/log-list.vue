<template>
  <view class="page">
    <view class="search-wrap">
      <u-search
        v-model="keyword"
        placeholder="搜索模块/用户/路径"
        show-action
        action-text="搜索"
        bg-color="#f5f6f8"
        @search="onSearch"
        @custom="onSearch" />
    </view>

    <view class="page-container">
      <u-loading-icon v-if="state.loading && !state.items.length" text="加载中..." />

      <view v-else-if="state.items.length">
        <view v-for="item in state.items" :key="item.id" class="card log-card">
          <view class="flex-row log-card__head">
            <u-tag :text="item.module" type="primary" plain size="mini" />
            <u-tag
              :text="String(item.statusCode)"
              :type="item.statusCode < 400 ? 'success' : 'error'"
              size="mini"
              custom-style="margin-left: 12rpx;" />
            <text class="log-card__time muted flex-1">{{ formatDateTime(item.createdAt) }}</text>
          </view>
          <text class="log-card__action">{{ item.action }}</text>
          <text class="log-card__path muted">{{ item.httpMethod }} {{ item.requestPath }}</text>
          <view class="log-card__meta flex-row">
            <text class="muted">操作人：{{ item.userName || '-' }}</text>
            <text class="muted flex-1 log-card__duration">{{ item.durationMs }} ms</text>
          </view>
        </view>
        <u-loadmore :status="loadStatus" custom-style="padding: 24rpx 0;" />
      </view>

      <empty-view v-else text="暂无日志数据" />
    </view>
  </view>
</template>

<script setup>
import { computed, reactive, ref } from 'vue';
import { onLoad, onPullDownRefresh, onReachBottom, onShow } from '@dcloudio/uni-app';
import EmptyView from '@/components/empty-view/empty-view.vue';
import env from '@/config/env';
import { createPagedState } from '@/model/page.model';
import * as logService from '@/service/log.service';
import { useUserStore } from '@/store/user';
import { formatDateTime } from '@/utils/format';

const userStore = useUserStore();
const keyword = ref('');
const state = reactive(createPagedState());

const loadStatus = computed(() => {
  if (state.loading) return 'loading';
  if (state.finished) return 'nomore';
  return 'loadmore';
});

onShow(() => {
  if (!userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/login/login' });
  }
});

onLoad(() => {
  if (!userStore.hasPermission('system.logs:view')) {
    uni.showToast({ title: '无权查看日志', icon: 'none' });
    return;
  }
  loadLogs(true);
});

onPullDownRefresh(async () => {
  await loadLogs(true);
  uni.stopPullDownRefresh();
});

onReachBottom(() => {
  if (!state.finished && !state.loading) {
    state.pageIndex += 1;
    loadLogs(false);
  }
});

function onSearch() {
  loadLogs(true);
}

async function loadLogs(reset) {
  if (reset) {
    state.pageIndex = 1;
    state.finished = false;
    state.items = [];
  }

  state.loading = true;
  try {
    const data = await logService.loadLogs({
      pageIndex: state.pageIndex,
      pageSize: env.defaultPageSize,
      keyword: keyword.value.trim()
    });
    const items = data?.items || [];
    state.total = data?.total || 0;
    state.items = reset ? items : [...state.items, ...items];
    state.finished = state.items.length >= state.total;
  } catch (err) {
    uni.showToast({ title: err.message || '加载失败', icon: 'none' });
  } finally {
    state.loading = false;
  }
}
</script>

<style scoped lang="scss">
.search-wrap {
  padding: 20rpx $page-padding;
  background: #fff;
  border-bottom: 1rpx solid $uni-border-color;
}

.log-card {
  &__head {
    margin-bottom: 12rpx;
  }

  &__time {
    text-align: right;
    font-size: 22rpx;
    margin-left: 12rpx;
  }

  &__action {
    display: block;
    font-size: 30rpx;
    font-weight: 600;
    color: $uni-text-color;
    line-height: 1.4;
  }

  &__path {
    display: block;
    margin-top: 10rpx;
    font-size: 24rpx;
    line-height: 1.5;
    word-break: break-all;
  }

  &__meta {
    margin-top: 16rpx;
    font-size: 24rpx;
  }

  &__duration {
    text-align: right;
  }
}
</style>
